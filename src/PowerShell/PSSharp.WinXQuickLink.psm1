function Invoke-WinXQuickLink
{
    [CmdletBinding(SupportsShouldProcess, RemotingCapability = 'PowerShell')]
    [OutputType([System.Management.Automation.Internal.AutomationNull])]
    param(
        [Parameter(ValueFromPipelineByPropertyName, Mandatory, ParameterSetName = 'FromName')]
        [PSSharp.WinXQuickLink.QuickLinkEntryNameCompletion()]
        [SupportsWildcards()]
        [string[]]
        $Name,

        [Parameter(ValueFromPipeline, Mandatory, ParameterSetName = 'FromValue')]
        [PSSharp.WinXQuickLink.QuickLinkEntry[]]
        $WinXQuickLink
    )
    process {
        if ($Name)
        {
            $WinXQuickLink += Get-WinXQuickLink -Name $Name
        }

        foreach ($QuickLink in $WinXQuickLink)
        {
            if ($PSCmdlet.ShouldProcess(
                "Invoking quick link '$($QuickLink.DisplayName)' (target: '$($QuickLink.TargetPath)').",
                "Invoke quick link '$($QuickLink.DisplayName)' (target: '$($QuickLink.TargetPath)')?",
                'Invoke QuickLink')) {
                Invoke-Item -Path $QuickLink.Path -WhatIf:$False -Confirm:$False
            }
        }
    }
}