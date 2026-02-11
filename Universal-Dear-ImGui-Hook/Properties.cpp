#include "Properties.h"
#include "MenuHelpers.h"
#include "pipe.h"

using namespace menu;

namespace Properties {

    std::string targetObjectId = "-1";
    float positionX = 0.0f;
    float positionY = 0.0f;
    float positionZ = 0.0f;
    float rotationX = 0.0f;
    float rotationY = 0.0f;
    float rotationZ = 0.0f;
    bool isModel = false;
    bool isPrevievPaste = false;

	void Render() {
        ImGui::SetNextWindowSize(ImVec2(300, 400), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowPos(ImVec2(490, 20), ImGuiCond_FirstUseEver);
        ImGuiWindowFlags newFlags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin("Properties", nullptr, newFlags))
        {
            ImGui::Text("Object ID: %s", targetObjectId.c_str());

            if (ImGui::CollapsingHeader("Transform", ImGuiTreeNodeFlags_DefaultOpen)) {

                ImGui::Text("Position");
                menu::FloatInput("X##pos_x", positionX, "properties_position_x", typing);
                menu::FloatInput("Y##pos_y", positionY, "properties_position_y", typing);
                menu::FloatInput("Z##pos_z", positionZ, "properties_position_z", typing);

                ImGui::Text("Rotation");
                menu::FloatInput("X##rot_x", rotationX, "properties_rotation_x", typing);
                menu::FloatInput("Y##rot_y", rotationY, "properties_rotation_y", typing);
                menu::FloatInput("Z##rot_z", rotationZ, "properties_rotation_z", typing);
            }

            if (isModel)
            if (ImGui::CollapsingHeader("Model")) {
                    
                if (ImGui::Button("Copy Cubes")) {
                    SendCommand("clipboard_copy_model");
                }
                if (ImGui::Button("Paste Cubes")) {
                    SendCommand("clipboard_paste_model");
                }

                if (ImGui::Checkbox("Preview Paste Cubes", &isPrevievPaste)) {
                    if (isPrevievPaste) SendCommand("clipboard_preview_paste_model|true");
                    else SendCommand("clipboard_preview_paste_model|false");
                }
            }
            ImGui::End();
        }
	}
}