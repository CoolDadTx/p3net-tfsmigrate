$logPath = "C:\temp"
$migratePath = "C:\Migration"

function Confirm ( [string] $message )
{
    do
    { 
        $input = Read-Host -prompt "$message (Y/N)? "
        if ($input -Like 'Y') { return $true }
        elseif ($input -Like 'N') { return $false }
    } while (true);
}

$postMigrateSteps = @()

cd $migratePath

# 1
if (-Not (Confirm('New project created'))) { exit }
$postMigrateSteps += "Add customized image"

# 2
if (-Not (Confirm('Process template customized'))) { exit }

# 3 - Code
if (Confirm('Migrate source code'))
{
    .\tfsmigrate.exe -processor VersionControl -logFile "$logPath\VersionControl.log" -verbose
    $postMigrateSteps += "Add root files"
    $postMigrateSteps += "Lock source code"
}

# 4 - Work items
if (Confirm('Migrate work items'))
{
    if (Confirm('Work items have been cleared') -and Confirm('Member of project Collection Service Accounts and has package management rights') -and Confirm('notifications for Work items disabled'))
    {
        .\tfsmigrate.exe -processor WorkItemTracking -logFile "$logpath\WorkItemTracking.log" -verbose

        $postMigrateSteps += "Set default iteration"
        $postMigrateSteps += "Set default area"
        $postMigrateSteps += "Lock work items"
        $postMigrateSteps += "Turn on notifications for Work items"
    }
}

# 5 - Extensions
$postMigrateSteps += "Install custom extensions"

# 6 - Queries
if (Confirm('Migrate queries (Y/N)?')) 
{
    if (Confirm('queries have been cleared'))
    {
        .\tfsmigrate.exe -processor QueryManagement -logFile "$logpath\QueryManagement.log" -verbose

        $postMigrateSteps += "Confirm migration of queries that cannot be automatically migrated"        
    }
}

# 7 - Dashboard
$postMigrateSteps += "Overview dashboard configured"

# 8 - Packages
if (Confirm('Migrate packages'))
{
    if (Confirm('Created package feed') -and Confirm('Updated settings file') - and Confirm('Added package source to command line'))
    {    
        .\tfsmigrate.exe -processor PackageManagement -logFile "$logpath\PackageManagement.log" -verbose
        $postMigrateSteps += "Tag packages"
    }
}

# 9 - Build Definitions
if (Confirm('Migrate build definitions'))
{
    if (Confirm('Imported task groups') -and Confirm('Updated settings with task group IDs'))
    {
        .\tfsmigrate.exe -processor BuildManagement -logFile "$logpath\BuildManagement.log" -verbose

        $postMigrateSteps += "Fix source paths"
        $postMigrateSteps += "Schedule builds"
    }
}

# 10 - Security
$postMigrateSteps += "Set up groups"
$postMigrateSteps += "Assign permissions and access levels"
$postMigrateSteps += "Configure notifications"
$postMigrateSteps += "Remove users from Service Accounts (tfssecurity /g- \"project Collection Service Accounts\" users /Collection:https://myaccount.visualstudio.com)"

# 11 - Post Migration
foreach ($msg in $postMigrateSteps) {
    Read-Host -Prompt $msg
}

