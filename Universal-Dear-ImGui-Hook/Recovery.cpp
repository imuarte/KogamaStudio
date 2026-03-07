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

            if (ImGui::Begin((std::string(T(u8"Recovery")) + u8"###Recovery").c_str(), nullptr))
            {
                if (ImGui::BeginTabBar(u8"Tabs"))
                {
                    if (ImGui::BeginTabItem(T(u8"Tools")))
                    {
                        menu::IntInput(T(u8"Target ItemID"), targetItemId, u8"recovery_target_item_id", typing);

                        if (ImGui::Button(T(u8"Remove Item From Inventory"))) SendCommand(u8"recovery_remove_item");

                        if (ImGui::Button(T(u8"Disable Loading Screen"))) SendCommand(u8"recovery_disable_loading_screen");
                        ImGui::EndTabItem();
                    }
                    if (ImGui::BeginTabItem(T(u8"Fixes")))
                    {

                        ImGui::EndTabItem();
                    }
                    if (ImGui::BeginTabItem(T(u8"Danger Zone")))
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
