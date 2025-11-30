# csproj에서 버전을 읽어서 자동으로 태그 생성 및 푸시

# csproj 파일 경로 (프로젝트 루트에서 실행 가정)
$csprojPath = "medipanda-windows-app/medipanda_windows_app.csproj"

# XML 파싱하여 Version 읽기
[xml]$csproj = Get-Content $csprojPath
$version = $csproj.Project.PropertyGroup.Version

if ([string]::IsNullOrEmpty($version)) {
    Write-Host "Version 정보를 찾을 수 없습니다." -ForegroundColor Red
    exit 1
}

$tag = "v$version"

Write-Host "버전: $version" -ForegroundColor Cyan
Write-Host "태그: $tag" -ForegroundColor Cyan
Write-Host ""

# 태그 생성
Write-Host "태그 생성 중..." -ForegroundColor Yellow
git tag -a $tag -m "Release $tag"

if ($LASTEXITCODE -ne 0) {
    Write-Host "태그 생성 실패" -ForegroundColor Red
    exit 1
}

# 태그 푸시
Write-Host "태그 푸시 중..." -ForegroundColor Yellow
git push origin $tag

if ($LASTEXITCODE -ne 0) {
    Write-Host "태그 푸시 실패" -ForegroundColor Red
    exit 1
}

Write-Host "완료: $tag 태그가 푸시되었습니다!" -ForegroundColor Green