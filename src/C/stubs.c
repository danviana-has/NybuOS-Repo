// Bare-metal native runtime stubs for Cosmos OS Gen 3

void RhDebugBreak(void) {
    __asm__ volatile("int3");
}
