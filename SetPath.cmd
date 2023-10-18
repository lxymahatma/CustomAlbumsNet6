@ECHO OFF
@REM This script must run as administrator, let user elevate
NET SESSION >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    GOTO AUTH
) ELSE (
    ECHO This script must be ran as adminstrator.
    PAUSE
    EXIT /B
)

@REM Sets the environment variable so we can use Directory.Build.Props
:AUTH
    SET /p "directory=Enter your Muse Dash directory in quotations: "
    SETX MD_NET6_DIRECTORY %directory%
    IF %ERRORLEVEL% NEQ 0 (
        ECHO An error occurred.
        PAUSE
        EXIT /B
    ) ELSE (
        ECHO Directory set successfully.
    )
    PAUSE
    EXIT /B