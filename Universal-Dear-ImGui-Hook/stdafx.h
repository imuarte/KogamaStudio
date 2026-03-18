#pragma once
#pragma comment(lib, "minhook/lib/libMinHook.x64.lib")

#pragma comment(lib, "dxgi.lib")
#pragma comment(lib, "d3d9.lib")
#pragma comment(lib, "d3d10.lib")
#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "d3d12.lib")
#pragma comment(lib, "vulkan-1.lib")

#include <windows.h>
#include <shlobj.h>
#include <vector>
#include <cstring>
#include <cstdio>
#include <cstdint>
#include <cstddef>
#include <cstdarg>

#include <dxgi.h>
#include <dxgi1_4.h>
#include <d3d9.h>
#include <d3d10_1.h>
#include <d3d10.h>
#include <d3d11.h>
#include <d3d12.h>
#include <vulkan/vulkan.h>

#include <wrl/client.h>

#if defined _M_X64
typedef uint64_t uintx_t;
#elif defined _M_IX86
typedef uint32_t uintx_t;
#endif

#include "imgui/imgui.h"
#include "imgui/imgui_impl_win32.h"
#include "imgui/imgui_impl_dx9.h"
#include "imgui/imgui_impl_dx10.h"
#include "imgui/imgui_impl_dx11.h"
#include "imgui/imgui_impl_dx12.h"
#include "imgui/imgui_impl_vulkan.h"

#include "minhook/include/MinHook.h"

#include "namespaces.h"
#include "UILocale.h"

// File-based logging + OutputDebugString
inline FILE* GetLogFile() {
    static FILE* logFile = nullptr;
    static bool attempted = false;
    if (!attempted) {
        attempted = true;
        char localAppData[MAX_PATH] = {};
        if (SUCCEEDED(SHGetFolderPathA(NULL, CSIDL_LOCAL_APPDATA, NULL, 0, localAppData))) {
            char baseDir[MAX_PATH];
            snprintf(baseDir, MAX_PATH, "%s\\KogamaStudio", localAppData);
            CreateDirectoryA(baseDir, NULL);
            char logDir[MAX_PATH];
            snprintf(logDir, MAX_PATH, "%s\\Logs", baseDir);
            CreateDirectoryA(logDir, NULL);
            char logPath[MAX_PATH];
            snprintf(logPath, MAX_PATH, "%s\\imgui-hook.log", logDir);
            fopen_s(&logFile, logPath, "w"); // overwrite each session
        }
    }
    return logFile;
}

inline void DebugLog(const char* fmt, ...) {
    char buf[1024];
    va_list args;
    va_start(args, fmt);
    vsnprintf(buf, sizeof(buf), fmt, args);
    va_end(args);

    // Always write to file
    FILE* f = GetLogFile();
    if (f) {
        SYSTEMTIME st;
        GetLocalTime(&st);
        fprintf(f, "[%02d:%02d:%02d.%03d] %s", st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, buf);
        fflush(f);
    }

    // Also to OutputDebugString if enabled
    if (globals::enableDebugLog) {
        OutputDebugStringA(buf);
    }
}
