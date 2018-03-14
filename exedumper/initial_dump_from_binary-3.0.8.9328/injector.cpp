#include <iostream>
#include <string>
#include <cstring>
#include <cstdio>
#include <thread>
#include <chrono>

#include <windows.h>
#include <tlhelp32.h>
#include <easyhook.h>
#include <tchar.h>

int wmain(int argc, WCHAR* argv[])
{
	if (argc < 3) {
		std::wcout << argv[0] << " dll command_line\n";
		return 1;
	}
	std::wstring dllToInject (argv[1]);
  std::wstring exe (argv[2]);
  exe = exe.substr (0, exe.find (' '));
  std::wstring command_line (argv[2]);
  command_line = command_line.substr (command_line.find (' ') + 1);
        
  ULONG pid;
	NTSTATUS nt = RhCreateAndInject
    ( const_cast<WCHAR*> (exe.c_str())
    , const_cast<WCHAR*> (command_line.c_str())
    , 0
		, EASYHOOK_INJECT_DEFAULT
		, const_cast<WCHAR*> (dllToInject.c_str())
		, nullptr
		, nullptr
		, 0
    , &pid
	  );

	if (nt != 0)
	{
		std::wcout << "RhCreateAndInject failed with error code = " << nt << "\n  " << RtlGetLastErrorString() << "\n";
		return 1;
	}
  	
	return 0;
}