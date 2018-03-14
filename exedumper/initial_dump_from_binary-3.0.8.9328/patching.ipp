
namespace 
{
  template<bool, typename> struct maybe_unprotect;
  template<typename T> struct maybe_unprotect<true, T>
  {
    static void apply(void* p, DWORD* old)
    {
      VirtualProtect(p, sizeof (T), PAGE_EXECUTE_READWRITE, old);
    }
    static void remove(void* p, DWORD old)
    {
      DWORD ignored;
      VirtualProtect(p, sizeof (T), old, &ignored);
    }
  };
  template<typename T> struct maybe_unprotect<false, T>
  {
    static void apply(void* p, DWORD* old)
    {
    }
    static void remove(void* p, DWORD old)
    {
    }
  };
}

namespace 
{
  char* module_base()
  {
    MODULEINFO info;
    GetModuleInformation(GetCurrentProcess(), GetModuleHandle (nullptr), &info, sizeof (info));
    return static_cast<char*> (info.lpBaseOfDll);
  }
  constexpr std::intptr_t const rebase_base(0x400000);
}

inline void* rebase (std::size_t offset)
{
  return static_cast<void*> (module_base() + offset - rebase_base);
}
inline std::size_t unrebase (void const* pointer)
{
  return static_cast<char const*> (pointer) - module_base() + rebase_base;
}

template<typename T, bool unprotect = true>
struct var
{
	size_t const _offset;
  
  constexpr var (size_t offset) : _offset (offset) {}

	T* _x = nullptr;
	DWORD old = 0;

	operator void*() { maybe_rebase(); return _x; }
	T& operator*() { maybe_rebase();  return *_x; }
	T* operator->() { maybe_rebase(); return _x; }

	void maybe_rebase()
	{
		if (_x) return;

		_x = static_cast<T*> (rebase (_offset));

		maybe_unprotect<unprotect, T>::apply(_x, &old);
	}
	~var()
	{
		if (_x) maybe_unprotect<unprotect, T>::remove(_x, old);
	}
};

template<typename Ret, typename... Args>
struct fun<Ret(Args...)> : var<Ret(Args...), false>
{
  using signature = Ret(Args...);

  using var<Ret(Args...), false>::var;
	using var<Ret(Args...), false>::operator void *;
	Ret operator() (Args... args)
	{
		maybe_rebase();
		return (*_x)(args...);
	}
};
template<typename Ret, typename T, typename... Args>
struct fun<Ret (T::*) (Args...)> : var<Ret(T*, Args...), false>
{
  using var<Ret(T*, Args...), false>::var;
	using var<Ret(T*, Args...), false>::operator void *;
	Ret operator() (T* t, Args... args)
	{
		maybe_rebase();
		return (*_x)(t, args...);
	}
};

template<typename Fun>
void hook (Fun& fun, typename Fun::signature* replacement, bool exclude_this_thread)
{
#define FORCE(what_,...)	\
	if (FAILED (__VA_ARGS__)) {\
		std::wcerr << "Failed to " << what_ << ": " << RtlGetLastErrorString() << "\n";\
		abort();\
}

	HOOK_TRACE_INFO hHook {0};
	FORCE ("install hook", LhInstallHook(fun, replacement, nullptr, &hHook));

	ULONG ACLEntries {0};
	FORCE("set hook acl", LhSetExclusiveACL(&ACLEntries, exclude_this_thread ? 1 : 0, &hHook));
}