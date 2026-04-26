// native_api.c
#include <Rinternals.h>

#ifndef _WIN32
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

#ifndef _WIN32
    // Unix/macOS: override the global console pointer
    ptr_R_WriteConsole = my_write_console;
#endif
}

int rbridge_typeof(SEXP x) {
    return TYPEOF(x);
}

SEXP rbridge_na_string() {
    return NA_STRING;
}

double rbridge_na_real() {
    return NA_REAL;
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