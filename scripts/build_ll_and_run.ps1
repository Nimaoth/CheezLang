&llc -filetype=obj -o gen/int/test.obj gen/int/test.ll
# &lld-link "/out:D:\Programming\CheezLang\gen\test_ll.exe" "-libpath:C:\Program Files (x86)\Windows Kits\10\Lib\10.0.18362.0\ucrt\x64" "-libpath:C:\Program Files (x86)\Windows Kits\10\Lib\10.0.18362.0\um\x64" "-libpath:C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.16.27023\lib\x64" "-libpath:D:\Programming\CheezLang\bin\Debug" "-libpath:D:\Programming\CheezLang\lib" "-libpath:D:\Programming\CheezLang\bin\Debug\lib" /entry:WinMainCRTStartup /machine:x64 /subsystem:console libucrtd.lib libcmtd.lib kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib legacy_stdio_definitions.lib legacy_stdio_wide_specifiers.lib libclang.lib libvcruntimed.lib msvcrtd.lib shlwapi.lib "D:\Programming\CheezLang\gen\int\test_ll.obj"
&lld-link "/out:D:\Programming\CheezLang\gen\test.exe" "-libpath:C:\Program Files (x86)\Windows Kits\10\Lib\10.0.18362.0\ucrt\x64" "-libpath:C:\Program Files (x86)\Windows Kits\10\Lib\10.0.18362.0\um\x64" "-libpath:C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.16.27023\lib\x64" "-libpath:D:\Programming\CheezLang\bin\Release" "-libpath:D:\Programming\CheezLang\lib" "-libpath:D:\Programming\CheezLang\bin\Release\lib" "/entry:WinMainCRTStartup" "/machine:x64" "/subsystem:console" "libucrtd.lib" "libcmtd.lib" "kernel32.lib" "user32.lib" "gdi32.lib" "winspool.lib" "comdlg32.lib" "advapi32.lib" "shell32.lib" "ole32.lib" "oleaut32.lib" "uuid.lib" "odbc32.lib" "odbccp32.lib" "legacy_stdio_definitions.lib" "legacy_stdio_wide_specifiers.lib" "libclang.lib" "libvcruntimed.lib" "msvcrtd.lib" "shlwapi.lib" "D:\Programming\CheezLang\examples\libraries\opengl\./lib/glad.lib" "D:\Programming\CheezLang\examples\libraries\opengl\./lib/Bindings.lib" "D:\Programming\CheezLang\examples\libraries\imgui\./lib/Bindings.lib" "D:\Programming\CheezLang\examples\libraries\imgui\./lib/imgui.lib" "D:\Programming\CheezLang\examples\libraries\glfw\./x64/glfw3dll.lib" "OpenGL32.Lib" "D:\Programming\CheezLang\gen\int\test.obj"
Push-Location gen
&"./test.exe"
Pop-Location