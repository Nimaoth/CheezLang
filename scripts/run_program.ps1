[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $file = "test"
)

Push-Location gen
&"./$file$executable_file_extension" -input "D:\Programming\C++ Projects\imgui\cimgui\cimgui.h" -out_path "D:\Programming\CheezLang\examples\libraries\imgui" -name cimgui_binding -no_includes
# &"./$file$executable_file_extension" -input "D:\llvm\include\clang-c\Index.h" -out_path . -name clang_c -no_enums -no_structs
# &"./$file$executable_file_extension" -input "../data/binding_test.cpp" -out_path . -name binding_test
# &"./$file$executable_file_extension" find-tokens -file D:\Programming\CheezLang\gen\uiae.che -type StringLiteral -suffix "c"
# &"./$file$executable_file_extension" compile -files test.che
# &"./$file$executable_file_extension" project new -name hello_world3 -type program
# &"./$file$executable_file_extension" help
# &"./$file$executable_file_extension" run -file main.che
# &"./$file$executable_file_extension"
Pop-Location

Write-Host "Program exited with code $LASTEXITCODE"