#include "stdafx.h"
#include "pipe.h"

using SetCursorPos_t = BOOL (WINAPI*)(int,int);
static SetCursorPos_t oSetCursorPos = nullptr;
BOOL WINAPI hookSetCursorPos(int x,int y) {
    if (menu::welcomeOpen) return TRUE;
    if (!pipe::cursorVisible) {
        return oSetCursorPos(x, y);
    }
    return menu::isOpen ? TRUE : oSetCursorPos(x,y);
}

using ClipCursor_t = BOOL (WINAPI*)(const RECT*);
static ClipCursor_t oClipCursor = nullptr;
BOOL WINAPI hookClipCursor(const RECT* rect) {
    if (menu::welcomeOpen) return TRUE;
    if (!pipe::cursorVisible) {
        return TRUE;
    }
    return menu::isOpen ? TRUE : oClipCursor(rect);
}

using GetCursorInfo_t = BOOL (WINAPI*)(PCURSORINFO);
static GetCursorInfo_t oGetCursorInfo = nullptr;
BOOL WINAPI hookGetCursorInfo(PCURSORINFO pci) {
    BOOL result = oGetCursorInfo(pci);
    if (result && pci && pipe::cursorVisible && menu::isOpen && !menu::viewportHovered) {
        pci->flags &= ~CURSOR_SHOWING;
        pci->hCursor = NULL;
    }
    return result;
}

namespace mousehooks {
    void Init() {
        HMODULE user32 = GetModuleHandleA("user32.dll");
        if (!user32)
            return;

        if (auto addr = GetProcAddress(user32, "SetCursorPos")) {
            if (MH_CreateHook(addr, reinterpret_cast<LPVOID>(hookSetCursorPos), reinterpret_cast<LPVOID*>(&oSetCursorPos)) == MH_OK) {
                MH_EnableHook(addr);
                DebugLog("[mousehooks] Hooked SetCursorPos@%p\n", addr);
            }
        }

        if (auto addr = GetProcAddress(user32, "ClipCursor")) {
            if (MH_CreateHook(addr, reinterpret_cast<LPVOID>(hookClipCursor), reinterpret_cast<LPVOID*>(&oClipCursor)) == MH_OK) {
                MH_EnableHook(addr);
                DebugLog("[mousehooks] Hooked ClipCursor@%p\n", addr);
            }
        }

        if (auto addr = GetProcAddress(user32, "GetCursorInfo")) {
            if (MH_CreateHook(addr, reinterpret_cast<LPVOID>(hookGetCursorInfo), reinterpret_cast<LPVOID*>(&oGetCursorInfo)) == MH_OK) {
                MH_EnableHook(addr);
                DebugLog("[mousehooks] Hooked GetCursorInfo@%p\n", addr);
            }
        }
    }

    void Remove() {
        HMODULE user32 = GetModuleHandleA("user32.dll");
        if (!user32)
            return;

        if (auto addr = GetProcAddress(user32, "SetCursorPos")) {
            MH_DisableHook(addr);
            MH_RemoveHook(addr);
        }

        if (auto addr = GetProcAddress(user32, "ClipCursor")) {
            MH_DisableHook(addr);
            MH_RemoveHook(addr);
        }

        if (auto addr = GetProcAddress(user32, "GetCursorInfo")) {
            MH_DisableHook(addr);
            MH_RemoveHook(addr);
        }
    }
}

