#include "stdafx.h"
#include "pipe.h"

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

namespace inputhook {
    static WNDPROC sOriginalWndProc = nullptr;

    bool remapCursor = false;

    static const float kViewportOffsetY = 10.0f;

    typedef BOOL(WINAPI* GetCursorPosFn)(LPPOINT);
    static GetCursorPosFn oGetCursorPos = nullptr;

    static POINT sCachedRemappedPt = {};
    static bool  sCachedValid = false;

    static BOOL WINAPI hookGetCursorPos(LPPOINT lpPoint)
    {
        BOOL result = oGetCursorPos(lpPoint);
        if (!result || !globals::mainWindow)
            return result;

        float imgW = menu::viewportImageMax.x - menu::viewportImageMin.x;
        float imgH = menu::viewportImageMax.y - menu::viewportImageMin.y;

        float screenX = (float)lpPoint->x;
        float screenY = (float)lpPoint->y;

        bool inViewport = imgW > 0.0f && imgH > 0.0f &&
                          screenX >= menu::viewportImageMin.x && screenX <= menu::viewportImageMax.x &&
                          screenY >= menu::viewportImageMin.y + kViewportOffsetY && screenY <= menu::viewportImageMax.y + kViewportOffsetY;

        if (!inViewport)
        {
            sCachedValid = false;
            return result;
        }

        // cursor is physically in viewport — if conditions aren't met, use last cached remap to avoid jumps
        if (!remapCursor || !menu::isOpen || !pipe::openMenu || !pipe::cursorVisible)
        {
            if (sCachedValid)
                *lpPoint = sCachedRemappedPt;
            return result;
        }

        RECT clientRect;
        GetClientRect(globals::mainWindow, &clientRect);
        POINT originPt = { 0, 0 };
        ClientToScreen(globals::mainWindow, &originPt);

        float relX = max(0.0f, min(1.0f, (screenX - menu::viewportImageMin.x) / imgW));
        float relY = max(0.0f, min(1.0f, (screenY - (menu::viewportImageMin.y + kViewportOffsetY)) / imgH));
        lpPoint->x = originPt.x + (LONG)roundf(relX * (float)clientRect.right);
        lpPoint->y = originPt.y + (LONG)roundf(relY * (float)clientRect.bottom - menu::menuBarHeight);

        sCachedRemappedPt = *lpPoint;
        sCachedValid = true;

        return result;
    }

    void InitGetCursorPosHook()
    {
        void* fn = GetProcAddress(GetModuleHandleA("user32.dll"), "GetCursorPos");
        if (fn)
        {
            MH_CreateHook(fn, hookGetCursorPos, reinterpret_cast<void**>(&oGetCursorPos));
            MH_EnableHook(fn);
        }
    }

    void RemoveGetCursorPosHook()
    {
        void* fn = GetProcAddress(GetModuleHandleA("user32.dll"), "GetCursorPos");
        if (fn)
        {
            MH_DisableHook(fn);
            MH_RemoveHook(fn);
            oGetCursorPos = nullptr;
        }
    }

    void Init(HWND hWindow)
    {
        DebugLog("[inputhook] Initializing input hook for window %p\n", hWindow);
        globals::mainWindow = hWindow;
        sOriginalWndProc = (WNDPROC)SetWindowLongPtr(hWindow, GWLP_WNDPROC, (LONG_PTR)WndProc);
        if (!sOriginalWndProc)
            DebugLog("[inputhook] Failed to set WndProc: %d\n", GetLastError());
        else
            DebugLog("[inputhook] WndProc hook set. Original WndProc=%p\n", sOriginalWndProc);
    }

    void Remove(HWND hWindow)
    {
        if (!sOriginalWndProc)
        {
            DebugLog("[inputhook] WndProc hook already removed or was never set\n");
            return;
        }
        DebugLog("[inputhook] Removing input hook for window %p\n", hWindow);
        if (SetWindowLongPtr(hWindow, GWLP_WNDPROC, (LONG_PTR)sOriginalWndProc) == 0)
            DebugLog("[inputhook] Failed to restore WndProc: %d\n", GetLastError());
        else
            DebugLog("[inputhook] WndProc restored to %p\n", sOriginalWndProc);
        sOriginalWndProc = nullptr;
        globals::mainWindow = nullptr;
    }

    LRESULT APIENTRY WndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
    {
        if (menu::isOpen && pipe::openMenu)
        {
            if (ImGui_ImplWin32_WndProcHandler(hwnd, uMsg, wParam, lParam))
                return TRUE;
            ImGuiIO& io = ImGui::GetIO();

            if (uMsg == WM_INPUT)
            {
                if (pipe::cursorVisible)
                {
                    POINT pt;
                    bool blocked = true;
                    if (oGetCursorPos && oGetCursorPos(&pt))
                    {
                        float imgW = menu::viewportImageMax.x - menu::viewportImageMin.x;
                        float imgH = menu::viewportImageMax.y - menu::viewportImageMin.y;
                        blocked = !(imgW > 0.0f && imgH > 0.0f &&
                            pt.x >= (LONG)menu::viewportImageMin.x && pt.x <= (LONG)menu::viewportImageMax.x &&
                            pt.y >= (LONG)menu::viewportImageMin.y && pt.y <= (LONG)menu::viewportImageMax.y);
                    }
                    if (blocked)
                        return DefWindowProcA(hwnd, uMsg, wParam, lParam);
                }
            }

            if (uMsg >= WM_MOUSEFIRST && uMsg <= WM_MOUSELAST)
            {
                int cx = (short)LOWORD(lParam);
                int cy = (short)HIWORD(lParam);

                POINT clientOrigin = { 0, 0 };
                ClientToScreen(hwnd, &clientOrigin);

                float screenX = (float)cx + (float)clientOrigin.x;
                float screenY = (float)cy + (float)clientOrigin.y;

                float imgW = menu::viewportImageMax.x - menu::viewportImageMin.x;
                float imgH = menu::viewportImageMax.y - menu::viewportImageMin.y;

                bool inViewport = imgW > 0.0f && imgH > 0.0f &&
                    screenX >= menu::viewportImageMin.x && screenX <= menu::viewportImageMax.x &&
                    screenY >= menu::viewportImageMin.y + kViewportOffsetY && screenY <= menu::viewportImageMax.y + kViewportOffsetY;

                if (inViewport)
                {
                    float relX = max(0.0f, min(1.0f, (screenX - menu::viewportImageMin.x) / imgW));
                    float relY = max(0.0f, min(1.0f, (screenY - (menu::viewportImageMin.y + kViewportOffsetY)) / imgH));
                    RECT clientRect;
                    GetClientRect(hwnd, &clientRect);
                    lParam = MAKELPARAM(
                        (int)roundf(relX * (float)clientRect.right),
                        (int)roundf(relY * (float)clientRect.bottom - menu::menuBarHeight)
                    );
                }
                else
                {
                    return TRUE;
                }
            }

            if (io.WantCaptureKeyboard && (uMsg >= WM_KEYFIRST && uMsg <= WM_KEYLAST))
                return TRUE;
            if (io.WantCaptureKeyboard && uMsg == WM_CHAR)
                return TRUE;
        }

        return CallWindowProc(sOriginalWndProc, hwnd, uMsg, wParam, lParam);
    }
}