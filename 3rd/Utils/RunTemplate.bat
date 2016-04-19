@echo off

SET FrameworkPath="%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.5"


IF "%CommonProgramFiles(x86)%"=="" (
	SET "BasePath=%CommonProgramFiles%"
) ELSE (
	SET "BasePath=%CommonProgramFiles(x86)%"
)

SET "TextTransformPath=%BasePath%\Microsoft Shared\TextTemplating\11.0\TextTransform.exe"

IF NOT EXIST "%TextTransformPath%" (
    SET "TextTransformPath=%BasePath%\Microsoft Shared\TextTemplating\12.0\TextTransform.exe"
)

IF NOT EXIST "%TextTransformPath%" (
    SET "TextTransformPath=%BasePath%\Microsoft Shared\TextTemplating\14.0\TextTransform.exe"
)

IF NOT EXIST "%TextTransformPath%" (
    SET "TextTransformPath=%BasePath%\Microsoft Shared\TextTemplating\10.0\TextTransform.exe"
)

IF NOT EXIST "%TextTransformPath%" (
echo "TextTemplating not found (correct for Win10)"
exit
)

echo Framework: %FrameworkPath%
echo Text Template: %TextTransformPath%

echo "%TextTransformPath%" -out "%~1.cs" -P "%2" -P %FrameworkPath% "%~1.tt"
"%TextTransformPath%" -out "%~1.cs" -P "%2" -P %FrameworkPath% "%~1.tt"