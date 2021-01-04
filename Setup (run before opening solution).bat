:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

@echo off
cd %~dp0
mklink /J GameBinaries "C:/Torch/server0/DedicatedServer64"
if errorlevel 1 goto Error
echo Done!
goto End
:Error
echo An error occured creating the symlink.
goto EndFinal
:End

cd %~dp0
mklink /J TorchBinaries "C:/Torch/server0"
if errorlevel 1 goto Error
echo Done! You can now open the Torch solution without issue.
goto EndFinal
:Error2
echo An error occured creating the symlink.
:EndFinal
pause
