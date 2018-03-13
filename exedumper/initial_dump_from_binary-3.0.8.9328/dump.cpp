#include <Windows.h>

BOOL APIENTRY DllMain (HMODULE, DWORD, LPVOID) { return TRUE; }

#include <easyhook.h>
#include <string>
#include <iostream>
#include <Psapi.h>
#include <thread>
#include <chrono>
#include <unordered_map>
#include <fstream>
#include <map>
#include <set>
#include <sstream>
#include <iomanip>

#include <boost/optional.hpp>

#include "patching.hpp"

using _UNKNOWN = void;
fun<void (int, const char *, const char **, _UNKNOWN *, char, _UNKNOWN *, _UNKNOWN *, unsigned int , unsigned int *, unsigned int *, char *, _UNKNOWN *)> sub_5B08F0
  = 0x5DEAD0;

fun<int()> sub_5B1AD0 = 0x5DF1D0;
fun<void()> sub_405AA0 = 0x406000;
  
void on_inject()
{
  //! This function is _not_ dbmeta but db update registration. the fields in here are in memory, not in file!
  hook (sub_5B08F0
  , [] ( int fieldCount, const char *structName, const char **fieldNames
       , _UNKNOWN *, char, _UNKNOWN *, _UNKNOWN *
       , unsigned int recordSize
       , unsigned int *fieldOffsets, unsigned int *fieldSizes, char *fieldTypesIsh
       , _UNKNOWN *someFunc
       )
  {
    std::ofstream(std::string (structName) + ".dbd");
    std::ofstream of(std::string (structName) + ".dbd", std::ios_base::app);
    of << "COLUMNS\n";
    for (int f = 0; f < fieldCount; ++f) {
      std::string type;
      switch(fieldTypesIsh[f]) {
        case 0:
          //! HACK: these are actually either int or float.
          type = "int";
          break;
        case 1:
          if (std::string(fieldNames[f]).find("_lang") == std::string::npos)
            type = "string";
          else
            //! HACK: these only work since in dbd we don't care for splitting them either
            type = "locstring";
          break;
        default:
          throw std::logic_error ("unknown typeish");
      }
      of << type << " " << (fieldNames[f] + 2) << "\n";
    }
    of << "\n";
    of << "BUILD 3.0.8.9328\n";
    
    for (int f = 0; f < fieldCount; ++f) {
      std::string suff;
      switch(fieldTypesIsh[f]) {
        case 0:
          if (fieldSizes[f] % 4 != 0) {
            suff = "<8>";
            if (fieldSizes[f] > 1) {
              suff += "[" + std::to_string (fieldSizes[f]) + "]";
            }
          } else {
            suff = "<32>";
            if (fieldSizes[f] / 4 > 1) {
              suff += "[" + std::to_string (fieldSizes[f] / 4) + "]";
            }
          }
          break;
        case 1:
          if (fieldSizes[f] % 4 != 0) throw std::logic_error ("non-4-byte-stringref");
          if (fieldSizes[f] / 4 > 1) {
            suff += "[" + std::to_string (fieldSizes[f] / 4) + "]";
          }
          break;
        default:
          throw std::logic_error ("unknown typeish");
      }
      of << (fieldNames[f] + 2) << suff << "\n";
    }
  }
  , false
  );
  
  hook (sub_405AA0, [] { sub_5B1AD0(); exit (0); });
}


extern "C" void __declspec(dllexport) __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo)
{
  on_inject();
    
  RhWakeUpProcess();
}
 