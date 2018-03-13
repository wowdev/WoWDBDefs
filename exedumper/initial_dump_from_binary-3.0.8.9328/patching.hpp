#pragma once

template<typename T, bool unprotect>
  struct var;
template<typename Fun> 
  struct fun;

template<typename Fun>
  void hook (Fun& fun, typename Fun::signature* replacement, bool exclude_this_thread = true);

void* rebase (size_t);
size_t unrebase (void const*);

#include "patching.ipp"