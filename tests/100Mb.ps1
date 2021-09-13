dotnet publish ../Altium.sln -o ../bin -c Release

$generatorDictionary = "Words.txt"
$unsortedFilePath = "100Mb_unsorted.txt"
$unsortedFileSize = 1024 * 1024 * 100
$bufferSize = 1024 * 1024 * 10
$bufferPath= "."

$Time = [System.Diagnostics.Stopwatch]::StartNew()

#dotnet ../bin/Generator.dll $unsortedFilePath $unsortedFileSize $generatorDictionary

$GenerationTime = $Time.Elapsed
write-host $([string]::Format("`rGeneration time: {0:d2}:{1:d2}:{2:d2}.{3:d3}", $GenerationTime.hours, $GenerationTime.minutes, $GenerationTime.seconds, $GenerationTime.milliseconds))

dotnet ../bin/Sort.dll $unsortedFilePath $bufferPath $bufferSize

$TotalTime = $Time.Elapsed
$SortingTime = $TotalTime - $GenerationTime
write-host $([string]::Format("`rSorting time: {0:d2}:{1:d2}:{2:d2}.{3:d3}", $SortingTime.hours, $SortingTime.minutes, $SortingTime.seconds, $SortingTime.milliseconds))
write-host $([string]::Format("`rTotal: {0:d2}:{1:d2}:{2:d2}.{3:d3}", $TotalTime.hours, $TotalTime.minutes, $TotalTime.seconds, $TotalTime.milliseconds))
