#include "Tools.h"
#include "MenuHelpers.h"
#include "pipe.h"

using namespace menu;

namespace Tools {

    static bool NoBuildLimit = false;
    static bool AntiAFK = false;
    static bool SingleSidePainting = false;
    static bool CustomGridSizeEnabled = false;
    static float CustomGridSizeValue = 1.0f;
    static bool CustomRotStepEnabled = false;
    static float CustomRotStepValue = 15.0f;
    static bool CustomSpeedEnabled = false;
    static float CustomSpeedValue = 2.0f;
    static bool UnlimitedConfigEnabled = false;
    static float UnlimitedConfigMin = 0.0f;
    static float UnlimitedConfigMax = 1.0f;
    static int WOId = -1;
    static float ObjectsRotation[3] = {0.0f, 0.0f, 0.0f};
    static char PlayerName[64] = "";
    static bool CustomModelScaleEnabled = false;
    static float CustomModelScaleValue = 4.0f;



    void Render(){

        // no build limit
        if (ImGui::Checkbox("No Build Limit", &NoBuildLimit)) {
            if (NoBuildLimit) SendCommand("option_no_build_limit|true");
            else SendCommand("option_no_build_limit|false");
        }

        // anti afk
        if (ImGui::Checkbox("Anti AFK", &AntiAFK)) {
            if (AntiAFK) SendCommand("option_anti_afk|true");
            else SendCommand("option_anti_afk|false");
        }

        // speed
        if (ImGui::Checkbox("Custom Speed", &CustomSpeedEnabled)) {
            if (CustomSpeedEnabled) SendCommand("option_custom_speed_enabled|true");
            else SendCommand("option_custom_speed_enabled|false");
        }

        if (CustomSpeedEnabled) {
            ImGui::PushItemWidth(100);
            ImGui::InputFloat("Speed", &CustomSpeedValue);
            ImGui::PopItemWidth();

            if (!typing) typing = ImGui::IsItemActive();

            if (ImGui::IsItemEdited()) {
                SendCommand(("option_custom_speed_value|" + std::to_string(CustomSpeedValue)).c_str());
            }
        }


        // custom grid size
        if (ImGui::Checkbox("Custom Grid Size", &CustomGridSizeEnabled)) {
            if (CustomGridSizeEnabled) SendCommand("option_custom_grid_size_enabled|true");
            else SendCommand("option_custom_grid_size_enabled|false");
        }

        if (CustomGridSizeEnabled) {
            ImGui::PushItemWidth(100);
            ImGui::InputFloat("Size", &CustomGridSizeValue);
            ImGui::PopItemWidth();

            if (!typing) typing = ImGui::IsItemActive();

            if (ImGui::IsItemEdited()) {
                SendCommand(("option_custom_grid_size|" + std::to_string(CustomGridSizeValue)).c_str());
            }
        }

        // custom rot step
        if (ImGui::Checkbox("Custom Rotation Step", &CustomRotStepEnabled)) {
            if (CustomRotStepEnabled) SendCommand("option_custom_rot_step_enabled|true");
            else SendCommand("option_custom_rot_step_enabled|false");
        }

        if (CustomRotStepEnabled) {
            ImGui::PushItemWidth(100);
            ImGui::InputFloat("Step", &CustomRotStepValue);
            ImGui::PopItemWidth();

            if (!typing) typing = ImGui::IsItemActive();

            if (ImGui::IsItemEdited()) {
                SendCommand(("option_custom_rot_step_size|" + std::to_string(CustomRotStepValue)).c_str());
            }
        }

        // unlimited config
        if (ImGui::Checkbox("Unlimited Config", &UnlimitedConfigEnabled)) {
            if (UnlimitedConfigEnabled) SendCommand("option_unlimited_config_enabled|true");
            else SendCommand("option_unlimited_config_enabled|false");
        }

        if (UnlimitedConfigEnabled) {
            ImGui::PushItemWidth(100);
            ImGui::InputFloat("Min##unlimited", &UnlimitedConfigMin);
            ImGui::PopItemWidth();
            if (!typing) typing = ImGui::IsItemActive();
            if (ImGui::IsItemEdited()) {
                SendCommand(("option_unlimited_config_min|" + std::to_string(UnlimitedConfigMin)).c_str());
            }

            ImGui::PushItemWidth(100);
            ImGui::InputFloat("Max##unlimited", &UnlimitedConfigMax);
            ImGui::PopItemWidth();
            if (!typing) typing = ImGui::IsItemActive();
            if (ImGui::IsItemEdited()) {
                SendCommand(("option_unlimited_config_max|" + std::to_string(UnlimitedConfigMax)).c_str());
            }
        }

        //custom model scale
        if (ImGui::Checkbox("Custom Model Scale", &CustomModelScaleEnabled)) {
            if (CustomModelScaleEnabled) SendCommand("option_custom_model_scale_enabled|true");
            else SendCommand("option_custom_model_scale_enabled|false");
        }

        if (CustomModelScaleEnabled) {
            ImGui::PushItemWidth(100);
            ImGui::InputFloat("Scale", &CustomModelScaleValue);
            ImGui::PopItemWidth();

            if (!typing) typing = ImGui::IsItemActive();

            if (ImGui::IsItemEdited()) {
                SendCommand(("option_custom_model_scale_value|" + std::to_string(CustomModelScaleValue)).c_str());
            }
        }

        // single side painting
        // cause crashes
        if (ImGui::Checkbox("##single_side", &SingleSidePainting)) {
            if (SingleSidePainting) SendCommand("option_single_side_painting|true");
            else SendCommand("option_single_side_painting|false");
        }
        ImGui::SameLine();
        ImGui::TextColored(ImVec4(1.0f, 1.0f, 0.0f, 1.0f), "Single Side Painting [EXPERIMENTAL]");

        if (ImGui::IsItemHovered())
            ImGui::SetTooltip("Enabling this may cause crashes.");

        //// objects
        //// get all wo ids
        //if (ImGui::Button("Get All Wo Ids")) SendCommand(("objects_get_all_wo_ids"));

        //ImGui::PushItemWidth(100);
        //ImGui::InputInt("Id", &WOId);
        //ImGui::PopItemWidth();

        //if (!typing) typing = ImGui::IsItemActive();

        //if (ImGui::IsItemEdited()) {
        //    SendCommand(("objects_wo_id|" + std::to_string(WOId)).c_str());
        //}

        //if (!typing) typing = ImGui::IsItemActive();

        ////clone
        //if (ImGui::Button("Clone")) SendCommand(("objects_clone"));

        ////visible
        //if (ImGui::Button("Toggle Visible")) SendCommand(("objects_visible"));

        //// remove
        //if (ImGui::Button("Remove")) SendCommand(("objects_remove"));

        ////rotation

        //ImGui::PushItemWidth(100);
        //ImGui::InputFloat3("Rotation", &ObjectsRotation[0]);
        //ImGui::PopItemWidth();

        //if (!typing) typing = ImGui::IsItemActive();

        //if (ImGui::IsItemEdited()) {
        //    SendCommand(("objects_rotation|" + std::to_string(CustomRotStepValue)).c_str());
        //}

        //// player name

        //ImGui::PushItemWidth(100);
        //ImGui::InputText("Player name", PlayerName, sizeof(PlayerName));
        //ImGui::PopItemWidth();

        //if (!typing) typing = ImGui::IsItemActive();

        //if (ImGui::IsItemEdited()) {
        //    SendCommand((std::string("player_name|") + PlayerName).c_str());
        //}

        //// test
        //if (ImGui::Button("Test")) SendCommand(("test"));
    }
}