// native_api.c
#include <Rinterface.h>

static void (*managed_write_console)(const char*, int) = 0;

void my_write_console(const char *buf, int len) {
    if (managed_write_console != 0) {
        managed_write_console(buf, len);
    }
}

void rbridge_set_write_console(void (*cb)(const char*, int)) {
    managed_write_console = cb;
    ptr_R_WriteConsole = my_write_console;
}
