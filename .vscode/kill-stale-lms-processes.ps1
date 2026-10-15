$ErrorActionPreference = 'SilentlyContinue'

$ports = @(5189, 7188)

foreach ($port in $ports) {
    $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue

    foreach ($listener in $listeners) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)" -ErrorAction SilentlyContinue

        if ($null -eq $process) {
            continue
        }

        $isProjectProcess = (
            $process.Name -eq 'LibraryManagementSystem.exe' -or
            ($process.Name -eq 'dotnet.exe' -and $process.CommandLine -match 'LibraryManagementSystem')
        )

        if ($isProjectProcess) {
            Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
}

exit 0
