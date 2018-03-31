Write-Host "Branch: $env:APPVEYOR_REPO_BRANCH"
Write-Host "PR Number: $env:APPVEYOR_PULL_REQUEST_NUMBER"
Write-Host "Job Name: $env:APPVEYOR_JOB_NAME"
if ( $env:git_access_token ) {
  Write-Host "Have Access Token"
} else {
  Write-Host "Do Not Have Access Token"
}

if($env:APPVEYOR_REPO_BRANCH -eq "master" -and -not $env:APPVEYOR_PULL_REQUEST_NUMBER -and $env:APPVEYOR_JOB_NAME -like "*Release*" -and $env:git_access_token){
  $Deploy="true"
  Write-Host "Deploying docs"
} else {
  Write-Host "Not deploying docs"
}

$docfxVersion = "2.33.0"
& nuget install docfx.console -Version $docfxVersion -Source https://api.nuget.org/v3/index.json

if($Deploy){
  # Configuring git credentials
  & git config --global credential.helper store
  Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:git_access_token):x-oauth-basic@github.com`n"

  & git config --global user.email "$env:git_email"
  & git config --global user.name "$env:git_user"
  & git config --global core.safecrlf false

  # Checkout gh-pages
  git clone --quiet --no-checkout --branch=gh-pages https://github.com/$($env:APPVEYOR_REPO_NAME) gh-pages
}

copy-item ..\README.md index.md
copy-item ..\*.md articles

& .\docfx.console.$docfxVersion\tools\docfx

if($Deploy){
  git -C gh-pages status
  $pendingChanges = git -C gh-pages status | select-string -pattern "Changes not staged for commit:","Untracked files:" -simplematch
  if ($pendingChanges -ne $null) {
      # Committing changes
      git -C gh-pages add -A
      git -C gh-pages commit -m "static site regeneration"
      # Pushing changes
      git -C gh-pages push --quiet origin gh-pages
      Write-Host "Docs deployed"
  }
  else {
      write-host "`nNo changes to documentation"
  }
}
