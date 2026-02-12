#include "Recovery.h"
#include "MenuHelpers.h"
#include "pipe.h"

using namespace menu;

namespace Recovery {

    bool recoveryOpen = false;
    int targetItemId = -1;

    void Render() {
        if (recoveryOpen) {
            ImGuiWindowFlags flags = ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoCollapse;
            ImGui::SetNextWindowSize(ImVec2(450, 600), ImGuiCond_FirstUseEver);
            ImGui::SetNextWindowPos(ImVec2((1920 / 2) - (450 / 2), (1080 / 2) - (600 / 2)), ImGuiCond_FirstUseEver);

            if (ImGui::Begin("Recovery", nullptr))
            {
                if (ImGui::BeginTabBar("Tabs"))
                {
                    if (ImGui::BeginTabItem("Tools"))
                    {
                        menu::IntInput("Target ItemID", targetItemId, "recovery_target_item_id", typing);

                        if (ImGui::Button("Remove Item From Inventory")) SendCommand("recovery_remove_item");

                        if (ImGui::Button("Disable Loading Screen")) SendCommand("recovery_disable_loading_screen");
                        ImGui::EndTabItem();
                    }
                    if (ImGui::BeginTabItem("Fixes"))
                    {

                        ImGui::EndTabItem();
                    }
                    if (ImGui::BeginTabItem("Danger Zone"))
                    {

                        ImGui::EndTabItem();
                    }
                    ImGui::EndTabBar();
                }
            }
            ImGui::End();

        }
    }

}