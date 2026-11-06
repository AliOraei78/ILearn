$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath (Split-Path $PSScriptRoot -Parent)).Path
$privatePaths = @('.env', 'ILearn.Android/keys', 'ILearn/App_Data')
$changed = 0
foreach ($relative in $privatePaths) {
    $root = Join-Path $workspace $relative
    if (!(Test-Path -LiteralPath $root)) { continue }
    $items = @(Get-Item -LiteralPath $root -Force)
    if ($items[0].PSIsContainer) { $items += @(Get-ChildItem -LiteralPath $root -Recurse -Force) }
    foreach ($item in $items) {
        if (!$item.FullName.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Refusing to change permissions outside the private project paths or through a link.'
        }
        if ([OperatingSystem]::IsWindows()) {
            # Modify only the DACL, preserving owner/auditing metadata and avoiding SeSecurityPrivilege.
            $acl = [IO.FileSystemAclExtensions]::GetAccessControl($item, [Security.AccessControl.AccessControlSections]::Access)
            $acl.SetAccessRuleProtection($true, $false)
            foreach ($rule in $acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])) { $acl.RemoveAccessRuleAll($rule) }
            $identities = @([Security.Principal.WindowsIdentity]::GetCurrent().User, [Security.Principal.SecurityIdentifier]::new('S-1-5-18'), [Security.Principal.SecurityIdentifier]::new('S-1-5-32-544'))
            foreach ($identity in $identities) {
                $rule = if ($item.PSIsContainer) {
                    [Security.AccessControl.FileSystemAccessRule]::new($identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
                } else { [Security.AccessControl.FileSystemAccessRule]::new($identity, 'FullControl', 'Allow') }
                $acl.AddAccessRule($rule)
            }
            [IO.FileSystemAclExtensions]::SetAccessControl($item, $acl)
        } else {
            $mode = [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite
            if ($item.PSIsContainer) { $mode = $mode -bor [IO.UnixFileMode]::UserExecute }
            [IO.File]::SetUnixFileMode($item.FullName, $mode)
        }
        $changed++
    }
}
Write-Output "Protected $changed private project files/directories; current user and system administrators retain access."
