#include "stdafx.h"
#include "pipe.h"

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

namespace inputhook {
    static WNDPROC sOriginalWndProc = nullptr;

    bool remapCursor = false;

    static float GetViewportOffsetY()
    {
        return pipe::isFullscreen ? 0.0f : 10.0f;
    }

    typedef BOOL(WINAPI* GetCursorPosFn)(LPPOINT);
    static GetCursorPosFn oGetCursorPos = nullptr;

    typedef SHORT(WINAPI* GetAsyncKeyStateFn)(int);
    static GetAsyncKeyStateFn oGetAsyncKeyState = nullptr;

    typedef SHORT(WINAPI* GetKeyStateFn)(int);
    static GetKeyStateFn oGetKeyState = nullptr;

    static bool ShouldBlockMouseButton(int vKey)
    {
        if (!(vKey == VK_LBUTTON || vKey == VK_RBUTTON || vKey == VK_MBUTTON))
            return false;
        if (menu::welcomeOpen)
            return true;
        return menu::isOpen && pipe::openMenu && menu::blockMouseInput;
    }

    static SHORT WINAPI hookGetAsyncKeyState(int vKey)
    {
        if (ShouldBlockMouseButton(vKey))
            return 0;
        return oGetAsyncKeyState(vKey);
    }

    static SHORT WINAPI hookGetKeyState(int vKey)
    {
        if (ShouldBlockMouseButton(vKey))
            return 0;
        return oGetKeyState(vKey);
    }

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

        // if menu closed or remapping disabled, unfreeze and pass through
        if (menu::welcomeOpen || !remapCursor || !menu::isOpen || !pipe::openMenu || !pipe::cursorVisible)
        {
            sCachedValid = false;
            return result;
        }

        bool inViewport = imgW > 0.0f && imgH > 0.0f &&
                          screenX >= menu::viewportImageMin.x && screenX <= menu::viewportImageMax.x &&
                          screenY >= menu::viewportImageMin.y + GetViewportOffsetY() && screenY <= menu::viewportImageMax.y + GetViewportOffsetY();

        if (!inViewport)
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
        float relY = max(0.0f, min(1.0f, (screenY - (menu::viewportImageMin.y + GetViewportOffsetY())) / imgH));
        lpPoint->x = originPt.x + (LONG)roundf(relX * (float)clientRect.right);
        lpPoint->y = originPt.y + (LONG)roundf(relY * (float)clientRect.bottom - menu::menuBarHeight) - (pipe::isFullscreen ? -18 : 0);

        sCachedRemappedPt = *lpPoint;
        sCachedValid = true;

        return result;
    }

    void InitGetCursorPosHook()
    {
        HMODULE user32 = GetModuleHandleA("user32.dll");

        void* fn = GetProcAddress(user32, "GetCursorPos");
        if (fn)
        {
            MH_CreateHook(fn, hookGetCursorPos, reinterpret_cast<void**>(&oGetCursorPos));
            MH_EnableHook(fn);
        }

        void* fnAKS = GetProcAddress(user32, "GetAsyncKeyState");
        if (fnAKS)
        {
            MH_CreateHook(fnAKS, hookGetAsyncKeyState, reinterpret_cast<void**>(&oGetAsyncKeyState));
            MH_EnableHook(fnAKS);
        }

        void* fnKS = GetProcAddress(user32, "GetKeyState");
        if (fnKS)
        {
            MH_CreateHook(fnKS, hookGetKeyState, reinterpret_cast<void**>(&oGetKeyState));
            MH_EnableHook(fnKS);
        }
    }

    void RemoveGetCursorPosHook()
    {
        HMODULE user32 = GetModuleHandleA("user32.dll");

        void* fn = GetProcAddress(user32, "GetCursorPos");
        if (fn)
        {
            MH_DisableHook(fn);
            MH_RemoveHook(fn);
            oGetCursorPos = nullptr;
        }

        void* fnAKS = GetProcAddress(user32, "GetAsyncKeyState");
        if (fnAKS)
        {
            MH_DisableHook(fnAKS);
            MH_RemoveHook(fnAKS);
            oGetAsyncKeyState = nullptr;
        }

        void* fnKS = GetProcAddress(user32, "GetKeyState");
        if (fnKS)
        {
            MH_DisableHook(fnKS);
            MH_RemoveHook(fnKS);
            oGetKeyState = nullptr;
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
        if ((menu::isOpen && pipe::openMenu) || menu::welcomeOpen)
        {
            if (ImGui_ImplWin32_WndProcHandler(hwnd, uMsg, wParam, lParam))
                return TRUE;

            // When welcome window is open, block all game input
            if (menu::welcomeOpen)
            {
                if ((uMsg >= WM_MOUSEFIRST && uMsg <= WM_MOUSELAST) ||
                    (uMsg >= WM_KEYFIRST && uMsg <= WM_KEYLAST) ||
                    uMsg == WM_CHAR || uMsg == WM_INPUT)
                    return TRUE;
                return CallWindowProc(sOriginalWndProc, hwnd, uMsg, wParam, lParam);
            }

            ImGuiIO& io = ImGui::GetIO();

            if (uMsg == WM_INPUT)
            {
                if (pipe::cursorVisible)
                {
                    POINT pt;
                    bool inViewport = false;
                    if (oGetCursorPos && oGetCursorPos(&pt))
                    {
                        float imgW = menu::viewportImageMax.x - menu::viewportImageMin.x;
                        float imgH = menu::viewportImageMax.y - menu::viewportImageMin.y;
                        inViewport = imgW > 0.0f && imgH > 0.0f &&
                            pt.x >= (LONG)menu::viewportImageMin.x && pt.x <= (LONG)menu::viewportImageMax.x &&
                            pt.y >= (LONG)menu::viewportImageMin.y && pt.y <= (LONG)menu::viewportImageMax.y;
                    }
                    if (!inViewport)
                        return DefWindowProcA(hwnd, uMsg, wParam, lParam);

                    // In viewport but ImGui has a window here — block mouse button raw events
                    if (io.WantCaptureMouse)
                    {
                        UINT sz = sizeof(RAWINPUT);
                        RAWINPUT ri{};
                        if (GetRawInputData((HRAWINPUT)lParam, RID_INPUT, &ri, &sz, sizeof(RAWINPUTHEADER)) != (UINT)-1)
                        {
                            if (ri.header.dwType == RIM_TYPEMOUSE && ri.data.mouse.usButtonFlags != 0)
                                return DefWindowProcA(hwnd, uMsg, wParam, lParam);
                        }
                    }
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
                    screenY >= menu::viewportImageMin.y + GetViewportOffsetY() && screenY <= menu::viewportImageMax.y + GetViewportOffsetY();

                if (inViewport)
                {
                    // Block clicks when an ImGui window sits over the viewport
                    if (io.WantCaptureMouse &&
                        (uMsg == WM_LBUTTONDOWN || uMsg == WM_LBUTTONUP   ||
                         uMsg == WM_RBUTTONDOWN || uMsg == WM_RBUTTONUP   ||
                         uMsg == WM_MBUTTONDOWN || uMsg == WM_MBUTTONUP   ||
                         uMsg == WM_LBUTTONDBLCLK || uMsg == WM_RBUTTONDBLCLK ||
                         uMsg == WM_MOUSEWHEEL  || uMsg == WM_MOUSEHWHEEL))
                        return TRUE;

                    float relX = max(0.0f, min(1.0f, (screenX - menu::viewportImageMin.x) / imgW));
                    float relY = max(0.0f, min(1.0f, (screenY - (menu::viewportImageMin.y + GetViewportOffsetY())) / imgH));
                    RECT clientRect;
                    GetClientRect(hwnd, &clientRect);
                    lParam = MAKELPARAM(
                        (int)roundf(relX * (float)clientRect.right),
                        (int)roundf(relY * (float)clientRect.bottom - menu::menuBarHeight) - (pipe::isFullscreen ? -18 : 0)
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