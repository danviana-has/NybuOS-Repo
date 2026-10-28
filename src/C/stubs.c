// Implementação de símbolos nativos do Native AOT para Cosmos OS
void RhDebugBreak(void) {
    __asm__ volatile("int $3");
}
