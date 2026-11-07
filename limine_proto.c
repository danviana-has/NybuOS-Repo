#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>
#include <stdnoreturn.h>
#include <config.h>
#include <lib/elf.h>
#include <lib/misc.h>
#include <lib/acpi.h>
#include <lib/config.h>
#include <lib/time.h>
#include <lib/pe.h>
#include <lib/print.h>
#include <lib/real.h>
#include <lib/libc.h>
#include <lib/gterm.h>
#include <lib/fdt.h>
#include <libfdt.h>
#include <lib/uri.h>
#include <sys/smp.h>
#include <sys/cpu.h>
#include <sys/gdt.h>
#include <lib/fb.h>
#include <lib/term.h>
#include <flanterm_backends/fb.h>
#include <sys/pic.h>
#include <sys/lapic.h>
#include <sys/iommu.h>
#include <sys/idt.h>
#include <fs/file.h>
#include <mm/pmm.h>
#include <pxe/tftp.h>
#include <drivers/edid.h>
#include <drivers/vga_textmode.h>
#include <lib/rand.h>
#define LIMINE_NO_POINTERS
#include <protos/limine.h>
#include <limine.h>

enum executable_format {
    EXECUTABLE_FORMAT_ELF,
    EXECUTABLE_FORMAT_PE,
};

static enum executable_format detect_kernel_format(uint8_t *kernel, size_t kernel_size) {
    if (elf_bits(kernel) != -1) {
        return EXECUTABLE_FORMAT_ELF;
    } else if (pe_bits(kernel, kernel_size) != -1) {
        return EXECUTABLE_FORMAT_PE;
    } else {
        panic(true, "limine: Unknown kernel executable format");
    }
}

#define SUPPORTED_BASE_REVISION 5

#define MAX_REQUESTS 128

#define MEMMAP_MAX 1024

static int paging_mode;

static uint64_t get_hhdm_span_top(int base_revision) {
    uint64_t ret = base_revision >= 3 ? 0 : 0x100000000;
    for (size_t i = 0; i < memmap_entries; i++) {
        if (((base_revision >= 1 && base_revision < 3) || base_revision >= 4) && (
            memmap[i].type == MEMMAP_RESERVED
         || memmap[i].type == MEMMAP_BAD_MEMORY)) {
            continue;
        }

        if (base_revision == 3 && (
            memmap[i].type != MEMMAP_USABLE
         && memmap[i].type != MEMMAP_BOOTLOADER_RECLAIMABLE
         && memmap[i].type != MEMMAP_KERNEL_AND_MODULES
         && memmap[i].type != MEMMAP_FRAMEBUFFER
         && memmap[i].type != MEMMAP_EFI_RECLAIMABLE)) {
            continue;
        }

        uint64_t base = memmap[i].base;
        uint64_t length = memmap[i].length;
        uint64_t top = base + length;

        if (base_revision < 3 && base < 0x100000000) {
            base = 0x100000000;
        }

        if (base >= top) {
            continue;
        }

        uint64_t aligned_top = ALIGN_UP(top, 0x40000000);

        if (aligned_top > ret) {
            ret = aligned_top;
        }
    }

    return ret;
}

#if defined (__i386__)
static pagemap_t build_identity_map(void) {
    pagemap_t pagemap = new_pagemap(paging_mode);

    map_pages(pagemap, 0, 0, VMM_FLAG_WRITE, 0x100000000);

    size_t _memmap_entries = memmap_entries;
    struct memmap_entry *_memmap =
        ext_mem_alloc(_memmap_entries * sizeof(struct memmap_entry));
    for (size_t i = 0; i < _memmap_entries; i++) {
        _memmap[i] = memmap[i];
    }

    for (size_t i = 0; i < _memmap_entries; i++) {
        if (_memmap[i].type != MEMMAP_USABLE
         && _memmap[i].type != MEMMAP_BOOTLOADER_RECLAIMABLE
         && _memmap[i].type != MEMMAP_KERNEL_AND_MODULES
         && _memmap[i].type != MEMMAP_FRAMEBUFFER
         && _memmap[i].type != MEMMAP_EFI_RECLAIMABLE) {
            continue;
        }

        uint64_t base   = _memmap[i].base;
        uint64_t length = _memmap[i].length;
        uint64_t top    = base + length;

        if (base < 0x100000000) {
            base = 0x100000000;
        }

        if (base >= top) {
            continue;
        }

        uint64_t aligned_base   = ALIGN_DOWN(base, 0x1000);
        uint64_t aligned_top    = ALIGN_UP(top, 0x1000);
        uint64_t aligned_length = aligned_top - aligned_base;

        map_pages(pagemap, aligned_base, aligned_base, VMM_FLAG_WRITE, aligned_length);
    }

    return pagemap;
}

void limine_memcpy_to_64_asm(int paging_mode, void *pagemap, uint64_t dst, void *src, size_t count);

static void limine_memcpy_to_64(uint64_t dst, void *src, size_t count) {
    static bool identity_map_ready = false;
    static pagemap_t identity_map;

    if (!identity_map_ready) {
        identity_map = build_identity_map();
        identity_map_ready = true;
    }

    limine_memcpy_to_64_asm(paging_mode, identity_map.top_level, dst, src, count);
}
#endif

static pagemap_t build_pagemap(int base_revision,
                               bool nx, struct mem_range *ranges, size_t ranges_count,
                               uint64_t physical_base, uint64_t virtual_base,
                               uint64_t direct_map_offset) {
    pagemap_t pagemap = new_pagemap(paging_mode);

    if (ranges_count == 0) {
        panic(true, "limine: ranges_count == 0");
    }

    for (size_t i = 0; i < ranges_count; i++) {
        uint64_t virt = ranges[i].base;
        uint64_t phys;

        if (virt & ((uint64_t)1 << 63)) {
            phys = physical_base + (virt - virtual_base);
        } else {
            panic(false, "limine: Virtual address of a PHDR in lower half");
        }

        uint64_t pf =
            (ranges[i].permissions & MEM_RANGE_X ? 0 : (nx ? VMM_FLAG_NOEXEC : 0)) |
            (ranges[i].permissions & MEM_RANGE_W ? VMM_FLAG_WRITE : 0);

        map_pages(pagemap, virt, phys, pf, ranges[i].length);
    }

    // Map 0x1000->4GiB range to identity if base revision == 0
    if (base_revision == 0) {
        map_pages(pagemap, 0x1000, 0x1000, VMM_FLAG_WRITE, 0x100000000 - 0x1000);
    }

    // Map 0->4GiB range to HHDM if base revision < 3
    if (base_revision < 3) {
        map_pages(pagemap, direct_map_offset, 0, VMM_FLAG_WRITE, 0x100000000);
    }

    size_t _memmap_entries = memmap_entries;
    struct memmap_entry *_memmap =
        ext_mem_alloc(_memmap_entries * sizeof(struct memmap_entry));
    for (size_t i = 0; i < _memmap_entries; i++)
        _memmap[i] = memmap[i];

    // Map all free memory regions to the higher half direct map offset
    for (size_t i = 0; i < _memmap_entries; i++) {
        if (((base_revision >= 1 && base_revision < 3) || base_revision >= 4) && (
            _memmap[i].type == MEMMAP_RESERVED
         || _memmap[i].type == MEMMAP_BAD_MEMORY)) {
            continue;
        }

        if (base_revision == 3 && (
            _memmap[i].type != MEMMAP_USABLE
         && _memmap[i].type != MEMMAP_BOOTLOADER_RECLAIMABLE
         && _memmap[i].type != MEMMAP_KERNEL_AND_MODULES
         && _memmap[i].type != MEMMAP_FRAMEBUFFER
         && _memmap[i].type != MEMMAP_EFI_RECLAIMABLE)) {
            continue;
        }

        uint64_t base   = _memmap[i].base;
        uint64_t length = _memmap[i].length;
        uint64_t top    = base + length;

        if (base_revision < 3 && base < 0x100000000) {
            base = 0x100000000;
        }

        if (base >= top) {
            continue;
        }

        uint64_t aligned_base   = ALIGN_DOWN(base, 0x1000);
        uint64_t aligned_top    = ALIGN_UP(top, 0x1000);
        uint64_t aligned_length = aligned_top - aligned_base;

        if (base_revision == 0) {
            map_pages(pagemap, aligned_base, aligned_base, VMM_FLAG_WRITE, aligned_length);
        }
        map_pages(pagemap, direct_map_offset + aligned_base, aligned_base, VMM_FLAG_WRITE, aligned_length);
    }

    // Map the framebuffer with appropriate permissions
    for (size_t i = 0; i < _memmap_entries; i++) {
        if (_memmap[i].type != MEMMAP_FRAMEBUFFER) {
            continue;
        }

        uint64_t base   = _memmap[i].base;
        uint64_t length = _memmap[i].length;
        uint64_t top    = base + length;

        uint64_t aligned_base   = ALIGN_DOWN(base, 0x1000);
        uint64_t aligned_top    = ALIGN_UP(top, 0x1000);
        uint64_t aligned_length = aligned_top - aligned_base;

        if (base_revision == 0) {
            map_pages(pagemap, aligned_base, aligned_base, VMM_FLAG_WRITE | VMM_FLAG_FB, aligned_length);
        }
        map_pages(pagemap, direct_map_offset + aligned_base, aligned_base, VMM_FLAG_WRITE | VMM_FLAG_FB, aligned_length);
    }

    // XXX we do this as a quick and dirty way to switch to the higher half
#if defined (__x86_64__) || defined (__i386__)
    if (base_revision >= 1) {
        map_pages(pagemap, 0, 0, VMM_FLAG_WRITE, 0x100000000);
    }
#endif

    return pagemap;
}

#if defined (__x86_64__) || defined (__i386__)
extern symbol limine_spinup_32;
#elif defined (__aarch64__)

#define LIMINE_SCTLR ((1 << 29) /* Res1 */                \
                    | (1 << 28) /* Res1 */                \
                    | (1 << 23) /* Res1 */                \
                    | (1 << 22) /* Res1 */                \
                    | (1 << 20) /* Res1 */                \
                    | (1 << 12) /* I-Cache */             \
                    | (1 << 11) /* Res1 */                \
                    | (1 << 8)  /* Res1 */                \
                    | (1 << 7)  /* Res1 */                \
                    | (1 << 4)  /* SP0 Alignment check */ \
                    | (1 << 3)  /* SP Alignment check */  \
                    | (1 << 2)  /* D-Cache */             \
                    | (1 << 0)) /* MMU */                 \

#define LIMINE_MAIR(fb) ( ((uint64_t)0b11111111 << 0) /* Normal WB RW-allocate non-transient */ \
                        | ((uint64_t)(fb) << 8) )     /* Framebuffer type */

#define LIMINE_TCR(tsz, pa) ( ((uint64_t)(pa) << 32)         /* Intermediate address size */  \
                            | ((uint64_t)2 << 30)            /* TTBR1 4K granule */           \
                            | ((uint64_t)2 << 28)            /* TTBR1 Outer shareable */      \
                            | ((uint64_t)1 << 26)            /* TTBR1 Outer WB RW-Allocate */ \
                            | ((uint64_t)1 << 24)            /* TTBR1 Inner WB RW-Allocate */ \
                            | ((uint64_t)(tsz) << 16)        /* Address bits in TTBR1 */      \
                                                             /* TTBR0 4K granule */           \
                            | ((uint64_t)2 << 12)            /* TTBR0 Outer shareable */      \
                            | ((uint64_t)1 << 10)            /* TTBR0 Outer WB RW-Allocate */ \
                            | ((uint64_t)1 << 8)             /* TTBR0 Inner WB RW-Allocate */ \
                            | ((uint64_t)(tsz) << 0))        /* Address bits in TTBR0 */

#elif defined (__riscv)
#elif defined (__loongarch64)
#else
#error Unknown architecture
#endif

static uint64_t physical_base, virtual_base, slide, direct_map_offset;
static size_t requests_count;
static void **requests;

static void set_paging_mode(bool randomise_hhdm_base) {
    direct_map_offset = paging_mode_higher_half(paging_mode);
    if (randomise_hhdm_base) {
        // A quarter of the higher half of wiggle room for KASLR, align to 1GiB steps.
        uint64_t mask = ((uint64_t)1 << (paging_mode_va_bits(paging_mode) - 3)) - 1;
        direct_map_offset += (rand64() & ~((uint64_t)0x40000000 - 1)) & mask;
    }
}

static uint64_t reported_addr(void *addr) {
    return (uint64_t)(uintptr_t)addr + direct_map_offset;
}

#if defined (__i386__)
static uint64_t reported_addr_64(uint64_t addr) {
    return addr + direct_map_offset;
}
#endif

#define get_phys_addr(addr) ({ \
    __auto_type get_phys_addr__addr = (addr); \
    uintptr_t get_phys_addr__r; \
    if (get_phys_addr__addr & ((uint64_t)1 << 63)) { \
        get_phys_addr__r = physical_base + (get_phys_addr__addr - virtual_base); \
    } else { \
        get_phys_addr__r = get_phys_addr__addr; \
    } \
    get_phys_addr__r; \
})

static struct limine_file get_file(struct file_handle *file, char *cmdline, bool kernel) {
    struct limine_file ret = {0};

    if (file->pxe) {
        ret.media_type = LIMINE_MEDIA_TYPE_TFTP;

        ret.tftp_ip = file->pxe_ip;
        ret.tftp_port = file->pxe_port;
    } else {
        struct volume *vol = file->vol;

        if (vol->is_optical) {
            ret.media_type = LIMINE_MEDIA_TYPE_OPTICAL;
        }

        ret.partition_index = vol->partition;

        ret.mbr_disk_id = mbr_get_id(vol->backing_dev ?: vol);

        if (vol->guid_valid) {
            memcpy(&ret.part_uuid, &vol->guid, sizeof(struct limine_uuid));
        }

        if (vol->part_guid_valid) {
            memcpy(&ret.gpt_part_uuid, &vol->part_guid, sizeof(struct limine_uuid));
        }

        struct guid gpt_disk_uuid;
        if (gpt_get_guid(&gpt_disk_uuid, vol->backing_dev ?: vol) == true) {
            memcpy(&ret.gpt_disk_uuid, &gpt_disk_uuid, sizeof(struct limine_uuid));
        }
    }

    char *path = ext_mem_alloc(file->path_len);
    memcpy(path, file->path, file->path_len);

    ret.path = reported_addr(path);

    void *freadall_ret = freadall_mode(file, MEMMAP_KERNEL_AND_MODULES, !kernel
#if defined (__i386__)
        , limine_memcpy_to_64
#endif
    );
#if defined (__i386__)
    ret.address = kernel ? reported_addr(freadall_ret) : reported_addr_64(*(uint64_t *)freadall_ret);
#else
    ret.address = reported_addr(freadall_ret);
#endif

    ret.size = file->size;

    ret.string = reported_addr(cmdline);

    return ret;
}

static void *_get_request(uint64_t id[4]) {
    for (size_t i = 0; i < requests_count; i++) {
        uint64_t *p = requests[i];

        if (p[2] != id[2]) {
            continue;
        }
        if (p[3] != id[3]) {
            continue;
        }

        return p;
    }

    return NULL;
}

#define get_request(REQ) _get_request((uint64_t[4])REQ)

#define FEAT_START do {
#define FEAT_END } while (0);

noreturn void limine_load(char *config, char *cmdline) {
#if defined (__x86_64__) || defined (__i386__)
    uint32_t eax, ebx, ecx, edx;
#endif

    char *kernel_path = config_get_value(config, 0, "PATH");
    if (kernel_path == NULL) {
        kernel_path = config_get_value(config, 0, "KERNEL_PATH");
    }
    if (kernel_path == NULL) {
        panic(true, "limine: Executable path not specified");
    }

    print("limine: Loading executable `%#`...\n", kernel_path);

    struct file_handle *kernel_file;
    if ((kernel_file = uri_open(kernel_path)) == NULL)
        panic(true, "limine: Failed to open executable with path `%#`. Is the path correct?", kernel_path);

    char *k_path_copy = ext_mem_alloc(strlen(kernel_path) + 1);
    strcpy(k_path_copy, kernel_path);
    char *k_resource = NULL, *k_root = NULL, *k_path = NULL, *k_hash = NULL;
    uri_resolve(k_path_copy, &k_resource, &k_root, &k_path, &k_hash);
    // Copy k_resource and k_root since uri_resolve returns pointers to a static
    // buffer that gets overwritten by subsequent uri_open/uri_resolve calls
    k_resource = strdup(k_resource);
    k_root = strdup(k_root);
    char *k_path_ = ext_mem_alloc(strlen(k_path) + 2);
    k_path_[0] = '/';
    strcpy(k_path_ + 1, k_path);
    k_path = k_path_;
    for (size_t i = strlen(k_path) - 1; ; i--) {
        if (k_path[i] == '/' || i == 1) {
            k_path[i] = 0;
            break;
        }
        k_path[i] = 0;
    }

    uint8_t *kernel = freadall(kernel_file, MEMMAP_BOOTLOADER_RECLAIMABLE);

    char *kaslr_s = config_get_value(config, 0, "KASLR");
    bool kaslr = false;
    if (kaslr_s != NULL && strcmp(kaslr_s, "yes") == 0) {
        kaslr = true;
    }

    // ELF loading
    uint64_t entry_point = 0;
    struct mem_range *ranges;
    uint64_t ranges_count;

    uint64_t image_size_before_bss;
    bool is_reloc;

    enum executable_format kernel_format = detect_kernel_format(kernel, kernel_file->size);
    switch (kernel_format) {
        case EXECUTABLE_FORMAT_ELF:
            if (!elf64_load(kernel, kernel_file->size, &entry_point, &slide,
                            MEMMAP_KERNEL_AND_MODULES, kaslr,
                            &ranges, &ranges_count,
                            &physical_base, &virtual_base, NULL,
                            &image_size_before_bss,
                            &is_reloc)) {
                panic(true, "limine: ELF64 load failure");
            }
            break;
        case EXECUTABLE_FORMAT_PE:
            if (!pe64_load(kernel, kernel_file->size, &entry_point, &slide,
                            MEMMAP_KERNEL_AND_MODULES, kaslr,
                            &ranges, &ranges_count,
                            &physical_base, &virtual_base, NULL,
                        