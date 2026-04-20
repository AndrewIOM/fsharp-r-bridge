// native_api.c
#include <Rinternals.h>

#ifdef _WIN32
extern void (*ptr_R_WriteConsole)(const char *, int);
#else
#include <Rinterface.h>
#endif

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

int rbridge_typeof(SEXP x) {
    return TYPEOF(x);
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

SEXP rbridge_get_car(SEXP node) {
    return CAR(node);
}

SEXP rbridge_get_cdr(SEXP node) {
    return CDR(node);
}

SEXP rbridge_get_tag(SEXP node) {
    return TAG(node);
}

SEXP rbridge_get_printname(SEXP sym) {
    return PRINTNAME(sym);
}