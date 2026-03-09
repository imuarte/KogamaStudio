#include "Properties.h"
#include "MenuHelpers.h"
#include "pipe.h"

using namespace menu;

namespace Properties {

    std::string targetObjectId = u8"-1";
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
        if (ImGui::Begin((std::string(T(u8"Object Properties")) + u8"###Properties").c_str(), nullptr, newFlags))
        {
            ImGui::Text(u8"%s: %s", T(u8"Object ID"), targetObjectId.c_str());

            if (ImGui::Button(T(u8"Remove"))) SendCommand(u8"properties_remove");
            ImGui::SameLine();
            if (ImGui::Button(T(u8"Teleport to"))) SendCommand((u8"recovery_teleport_to_object|" + targetObjectId).c_str());
            ImGui::SameLine();
            if (ImGui::Button(T(u8"Add to Inventory"))) SendCommand((u8"inventory_add_object|" + targetObjectId).c_str());

            if (ImGui::CollapsingHeader(T(u8"Transform"), ImGuiTreeNodeFlags_DefaultOpen)) {

                ImGui::Text(T(u8"Position"));
                menu::DragFloatInput(u8"X##pos_x", positionX, u8"properties_position_x", typing, 0.1f);
                menu::DragFloatInput(u8"Y##pos_y", positionY, u8"properties_position_y", typing, 0.1f);
                menu::DragFloatInput(u8"Z##pos_z", positionZ, u8"properties_position_z", typing, 0.1f);

                ImGui::Text(T(u8"Rotation"));
                menu::DragFloatInput(u8"X##rot_x", rotationX, u8"properties_rotation_x", typing, 1.0f);
                menu::DragFloatInput(u8"Y##rot_y", rotationY, u8"properties_rotation_y", typing, 1.0f);
                menu::DragFloatInput(u8"Z##rot_z", rotationZ, u8"properties_rotation_z", typing, 1.0f);
            }

            if (isModel)
            if (ImGui::CollapsingHeader(T(u8"Model"))) {

                if (ImGui::Button(T(u8"Copy Cubes"))) {
                    SendCommand(u8"clipboard_copy_model");
                }
                if (ImGui::Button(T(u8"Paste Cubes"))) {
                    SendCommand(u8"clipboard_paste_model");
                }

                if (ImGui::Checkbox(T(u8"Preview Paste Cubes"), &isPrevievPaste)) {
                    if (isPrevievPaste) SendCommand(u8"clipboard_preview_paste_model|true");
                    else SendCommand(u8"clipboard_preview_paste_model|false");
                }
            }
        }
        ImGui::End();
	}
}
