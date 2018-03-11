if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) 
{
	"Run as Administrator, or inject will fail"
	exit
}

$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$script_dir = (split-path $MyInvocation.MyCommand.Path) 

$msvcpp = "Visual Studio 15 2017"
$msvs_short = "vs2017"

if(!(Test-Path -Path $script_dir/initial_dump_from_binary-3.0.1.8303-build)) {
	mkdir initial_dump_from_binary-3.0.1.8303-build
	cd initial_dump_from_binary-3.0.1.8303-build

    if(-not (Test-Path nuget.exe))
    {
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile nuget.exe
    }

    & "./nuget.exe" install EasyHookNativePackage

	& cmake -DCMAKE_INSTALL_PREFIX="${script_dir}/initial_dump_from_binary-3.0.1.8303-install" -G"$msvcpp" ../initial_dump_from_binary-3.0.1.8303
	cd ..
}

if(Test-Path -Path $script_dir/initial_dump_from_binary-3.0.1.8303-install) {
	Remove-Item -Path "${script_dir}/initial_dump_from_binary-3.0.1.8303-install" -Confirm:$false -Force -Recurse
}
& cmake --build $script_dir/initial_dump_from_binary-3.0.1.8303-build --config Release --target install

& "${script_dir}/initial_dump_from_binary-3.0.1.8303-install/injector.exe" "${script_dir}/initial_dump_from_binary-3.0.1.8303-install/dump.dll" "WOW-8303patch3.0.1_WIN.exe" 2>&1

Start-Sleep -Seconds 2