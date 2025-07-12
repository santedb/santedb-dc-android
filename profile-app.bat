adb reverse tcp:9000 tcp:9001
adb shell setprop debug.mono.profile '127.0.0.1:9000,nosuspend,connect'
dotnet-trace collect --dsrouter android --format speedscope
adb shell setprop debug.mono.profile ''