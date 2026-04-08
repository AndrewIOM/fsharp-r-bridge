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

// Set the CAR (value) of a pairlist node
void rbridge_set_car(SEXP node, SEXP value) {
    SETCAR(node, value);
}

// Set the CDR (next node) of a pairlist node
void rbridge_set_cdr(SEXP node, SEXP next) {
    SETCDR(node, next);
}

// Set the TAG (argument name) of a pairlist node
void rbridge_set_tag(SEXP node, SEXP tag) {
    SET_TAG(node, tag);
}
