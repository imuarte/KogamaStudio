#include "stdafx.h"
#include "ResourcePackLoader.h"
#include "pipe.h"
#include "MenuHelpers.h"
#include <chrono>
#include <thread>
#include <filesystem>
#include "Tools.h"
#include "ResourcePacks.h"
#include "Generate.h"
#include "About.h"
#include "Translate.h"

namespace menu {
    bool isOpen = false;
    float test = 0.0f;
    static bool noTitleBar = false;
    static HMODULE hModule = nullptr;

    void Init() {
        static float scale = 1.0f;

        static bool initializationNotification = false;

        if (!hModule) {
            hModule = GetModuleHandle("KogamaStudio.dll");
            if (hModule) {
                DebugLog("[menu] KogamaStudio.dll loaded\n");
            }
        }

        // Begin frame UI setup
        DebugLog("[menu] Rendering menu. isOpen=%d, test=%.2f\n", isOpen, test);

        ImGuiIO& io = ImGui::GetIO();

        if (!isOpen) {
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
            ImGui::StyleColorsDark();
            ImVec4* colors = ImGui::GetStyle().Colors;

            ImGui::GetStyle().ScaleAllSizes(scale);

            styled = true;
            DebugLog("[menu] Style applied.\n");
        }

        // Window flags
        ImGuiWindowFlags flags = ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoCollapse;
        ImGui::SetNextWindowSize(ImVec2(450, 600), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowPos(ImVec2(20, 20), ImGuiCond_FirstUseEver);

        if (ImGui::Begin("KogamaStudio", nullptr))
        {

            if (ImGui::BeginTabBar("Tabs"))
            {
                if (ImGui::BeginTabItem("Tools"))
                {
                    Tools::Render();
                    ImGui::EndTabItem();
                }

                // RESOURCE PACKS
                if (ImGui::BeginTabItem("Resource Packs"))
                {
                    ResourcePacks::Render();
                    ImGui::EndTabItem();
                }

                if (ImGui::BeginTabItem("Generate"))
                {
                    Generate::Render();
                    ImGui::EndTabItem();
                }

                if (ImGui::BeginTabItem("Translate"))
                {
                    Translate::Render();
                    ImGui::EndTabItem();
                }

                if (ImGui::BeginTabItem("About"))
                {
                    About::Render();
                    ImGui::EndTabItem();
                }

                ImGui::EndTabBar();
            }
        }
        ImGui::End();
    }
}
