#include "stdafx.h"
#include "imgui/imgui_internal.h"
#include "ResourcePackLoader.h"
#include "pipe.h"
#include "MenuHelpers.h"
#include <chrono>
#include <thread>
#include <filesystem>
#include "Tools.h"
#include "ResourcePacks.h"
#include "About.h"
#include "Translate.h"
#include "Properties.h"
#include "Clipboard.h"
#include "Appearance.h"
#include "Recovery.h"
#include "Console.h"
#include "Players.h"
#include "Camera.h"
#include "Explorer.h"
#include "GameInfo.h"
#include "Inventory.h"
#include "History.h"
#include "Worlds.h"

namespace menu {
    bool isOpen = false;
    bool viewportHovered = false;
    bool blockMouseInput = false;
    ImVec2 viewportImageMin = {};
    ImVec2 viewportImageMax = {};
    float menuBarHeight = 0.0f;
    float test = 0.0f;
    static bool noTitleBar = false;
    static HMODULE hModule = nullptr;

    namespace Windows {
        static bool appearanceOpen = false;
        static bool consoleOpen = false;
    }

    void Init() {
        static float scale = 1.0f;

        static bool initializationNotification = false;

        if (!hModule) {
            hModule = GetModuleHandle(u8"KogamaStudio.dll");
            if (hModule) {
                DebugLog(u8"[menu] KogamaStudio.dll loaded\n");
            }
        }

        // Begin frame UI setup
        DebugLog(u8"[menu] Rendering menu. isOpen=%d, test=%.2f\n", isOpen, test);

        ImGuiIO& io = ImGui::GetIO();

        Recovery::RenderOverlay();

        if (!isOpen) {
            viewportHovered = false;
            return;
        }

        if (!pipe::cursorVisible) {
            RECT rect;
            GetWindowRect(GetForegroundWindow(), &rect);
            int centerX = (rect.left + rect.right) / 2;
            int centerY = (rect.top + rect.bottom) / 2;
            SetCursorPos(centerX, centerY);
        }

        if (!pipe::openMenu) {
            viewportImageMin = {};
            viewportImageMax = {};
            return;
        }

        if (typing) {
            io.WantCaptureKeyboard = true;
        }
        else {
            io.WantCaptureKeyboard = false;
        }
        typing = false;


        // Style setup (one-time)
        static bool styled = false;
        if (!styled) {
            Appearance::LoadSettings();
            ImGui::GetStyle().ScaleAllSizes(scale);
            styled = true;
            DebugLog(u8"[menu] Style applied.\n");
        }

        ImGuiViewport* mainViewport = ImGui::GetMainViewport();
        ImGui::SetNextWindowPos(mainViewport->WorkPos);
        ImGui::SetNextWindowSize(mainViewport->WorkSize);
        ImGui::SetNextWindowViewport(mainViewport->ID);
        ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, 0.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_WindowBorderSize, 0.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0.0f, 0.0f));
        ImGui::Begin(u8"##DockSpaceHost", nullptr,
            ImGuiWindowFlags_NoDocking | ImGuiWindowFlags_NoTitleBar |
            ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoMove |
            ImGuiWindowFlags_NoBringToFrontOnFocus | ImGuiWindowFlags_NoNavFocus);
        ImGui::PopStyleVar(3);
        ImGuiID dockId = ImGui::GetID(u8"MainDockSpace");
        ImGui::DockSpace(dockId, ImVec2(0.0f, 0.0f));

        static bool dockLayoutBuilt = false;
        if (!dockLayoutBuilt)
        {
            dockLayoutBuilt = true;
            if (ImGui::DockBuilderGetNode(dockId) != nullptr)
                goto dockLayoutDone; // layout loaded from ini, skip default build
            ImGui::DockBuilderRemoveNode(dockId);
            ImGui::DockBuilderAddNode(dockId, ImGuiDockNodeFlags_DockSpace);
            ImGui::DockBuilderSetNodeSize(dockId, mainViewport->WorkSize);

            ImGuiID dockRight, dockLeft;
            ImGui::DockBuilderSplitNode(dockId, ImGuiDir_Right, 0.32f, &dockRight, &dockLeft);

            ImGuiID dockRightTop, dockRightBot;
            ImGui::DockBuilderSplitNode(dockRight, ImGuiDir_Down, 0.5f, &dockRightBot, &dockRightTop);

            ImGuiID dockRightMid, dockRightBottom;
            ImGui::DockBuilderSplitNode(dockRightBot, ImGuiDir_Down, 0.5f, &dockRightBottom, &dockRightMid);

            ImGui::DockBuilderDockWindow(u8"###Viewport",      dockLeft);
            // right top: explorer + tools
            ImGui::DockBuilderDockWindow(u8"###Explorer",      dockRightTop);
            ImGui::DockBuilderDockWindow(u8"###Tools",         dockRightTop);
            // right mid: properties + clipboard + history
            ImGui::DockBuilderDockWindow(u8"###Properties",    dockRightMid);
            ImGui::DockBuilderDockWindow(u8"###Clipboard",     dockRightMid);
            ImGui::DockBuilderDockWindow(u8"###History",       dockRightMid);
            // right bottom: secondary panels
            ImGui::DockBuilderDockWindow(u8"###GameInfo",      dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###Inventory",     dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###Players",       dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###Camera",        dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###ResourcePacks", dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###Translate",     dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###About",         dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###Console",       dockRightBottom);
            ImGui::DockBuilderDockWindow(u8"###Worlds",        dockRightBottom);
            ImGui::DockBuilderFinish(dockId);
            dockLayoutDone:;
        }

        ImGui::End();

        ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0.0f, 0.0f));
        ImGui::SetNextWindowSize(ImVec2(800, 600), ImGuiCond_FirstUseEver);
        if (ImGui::Begin((std::string(T(u8"Viewport")) + u8"###Viewport").c_str(), nullptr, ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoScrollWithMouse))
        {
            ImVec2 contentSize = ImGui::GetContentRegionAvail();
            if (contentSize.x > 0 && contentSize.y > 0)
            {
                // Letterbox: scale game frame to fit panel while preserving aspect ratio
                UINT frameW = 0, frameH = 0;
                hooks_dx11::GetGameFrameSize(frameW, frameH);
                float aspect = (frameW > 0 && frameH > 0) ? (float)frameW / (float)frameH : 16.0f / 9.0f;

                float imgW = contentSize.x;
                float imgH = contentSize.x / aspect;
                if (imgH > contentSize.y) { imgH = contentSize.y; imgW = contentSize.y * aspect; }

                float offX = (contentSize.x - imgW) * 0.5f;
                float offY = (contentSize.y - imgH) * 0.5f;
                ImGui::SetCursorPos(ImVec2(ImGui::GetCursorPosX() + offX, ImGui::GetCursorPosY() + offY));

                ID3D11ShaderResourceView* srv = hooks_dx11::GetGameFrameSRV();
                if (srv)
                {
                    ImGui::Image((ImTextureID)(void*)srv, ImVec2(imgW, imgH));
                    viewportHovered = ImGui::IsItemHovered();
                    viewportImageMin = ImGui::GetItemRectMin();
                    viewportImageMax = ImGui::GetItemRectMax();
                    if (ImGui::BeginDragDropTarget())
                    {
                        if (const ImGuiPayload* payload = ImGui::AcceptDragDropPayload(u8"INVENTORY_ITEM"))
                            SendCommand((std::string(u8"world_place_item|") + (const char*)payload->Data).c_str());
                        ImGui::EndDragDropTarget();
                    }
                }
                else
                {
                    viewportHovered = false;
                    viewportImageMin = {};
                    viewportImageMax = {};
                }
            }
        }
        ImGui::End();
        ImGui::PopStyleVar();

        static bool prevViewportHovered = false;
        if (viewportHovered != prevViewportHovered)
        {
            prevViewportHovered = viewportHovered;
            SendCommand(viewportHovered ? u8"viewport_cursor|false" : u8"viewport_cursor|true");
        }

        // --- Main panels (all manage their own Begin/End) ---
        Explorer::Render();
        Tools::Render();

        Properties::Render();
        Clipboard::Render();
        History::Render();

        GameInfo::Render();
        Inventory::Render();
        Players::Render();
        CameraPanel::Render();
        ResourcePacks::Render();
        Translate::Render();
        About::Render();
        Worlds::Render();

        if (ImGui::BeginMainMenuBar())
        {
            menuBarHeight = ImGui::GetWindowSize().y;
            if (ImGui::BeginMenu(T(u8"Windows")))
            {
                ImGui::MenuItem(T(u8"Appearance"), nullptr, &Windows::appearanceOpen);
                ImGui::MenuItem(T(u8"Console"), nullptr, &Windows::consoleOpen);
                ImGui::EndMenu();
            }
            ImGui::EndMainMenuBar();
        }

        if (Windows::appearanceOpen) Appearance::Render();
        if (Windows::consoleOpen) AppLog::Render();

        Recovery::Render();
    }
}
