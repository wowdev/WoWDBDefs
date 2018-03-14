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

if(!(Test-Path -Path $script_dir/initial_dump_from_binary-3.0.8.9328-build)) {
	mkdir initial_dump_from_binary-3.0.8.9328-build
	cd initial_dump_from_binary-3.0.8.9328-build

    if(-not (Test-Path nuget.exe))
    {
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile nuget.exe
    }

    & "./nuget.exe" install EasyHookNativePackage

	& cmake -DCMAKE_INSTALL_PREFIX="${script_dir}/initial_dump_from_binary-3.0.8.9328-install" -G"$msvcpp" ../initial_dump_from_binary-3.0.8.9328
	cd ..
}

if(Test-Path -Path $script_dir/initial_dump_from_binary-3.0.8.9328-install) {
	Remove-Item -Path "${script_dir}/initial_dump_from_binary-3.0.8.9328-install" -Confirm:$false -Force -Recurse
}
& cmake --build $script_dir/initial_dump_from_binary-3.0.8.9328-build --config Release --target install

& "${script_dir}/initial_dump_from_binary-3.0.8.9328-install/injector.exe" "${script_dir}/initial_dump_from_binary-3.0.8.9328-install/dump.dll" "WOW-9327patch3.0.8_PTR_WIN.exe" 2>&1

Start-Sleep -Seconds 2