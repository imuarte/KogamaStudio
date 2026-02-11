#include "Properties.h"
#include "MenuHelpers.h"
#include "pipe.h"

using namespace menu;

namespace Clipboard {

    int offsetX = 0.0f;
    int offsetY = 0.0f;
    int offsetZ = 0.0f;
    int rotationX = 0;
    int rotationY = 0;
    int rotationZ = 0;
    bool mirrorX = 0;
    bool mirrorY = 0;
    bool mirrorZ = 0;
    int clipMinX = 0, clipMaxX = 0;
    int clipMinY = 0, clipMaxY = 0;
    int clipMinZ = 0, clipMaxZ = 0;

    void Render() {
        ImGui::SetNextWindowSize(ImVec2(300, 400), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowPos(ImVec2(800, 20), ImGuiCond_FirstUseEver);
        ImGuiWindowFlags newFlags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin("Clipboard", nullptr, newFlags))
        {
            if (ImGui::CollapsingHeader("Transform")) {
                // OFFSET
                ImGui::Text("Offset");
                menu::IntInput("X##offset_x", offsetX, "clipboard_offset_x", typing);
                menu::IntInput("Y##offset_y", offsetY, "clipboard_offset_y", typing);
                menu::IntInput("Z##offset_z", offsetZ, "clipboard_offset_z", typing);

                // ROTATION
                const char* rotationOptions[] = { u8"0°", u8"90°", u8"180°", u8"270°" };
                ImGui::Text("Rotation");
                /*
                ImGui::Combo("X##rot_x", &rotationX, rotationOptions, 4);
                if (ImGui::IsItemEdited()) {
                    SendCommand(("clipboard_rotation_x|" + std::to_string(rotationX * 90)).c_str());
                }
                */
                ImGui::Combo("Y##rot_y", &rotationY, rotationOptions, 4);
                if (ImGui::IsItemEdited()) {
                    SendCommand(("clipboard_rotation_y|" + std::to_string(rotationY * 90)).c_str());
                }
                /*
                ImGui::Combo("Z##rot_z", &rotationZ, rotationOptions, 4);
                if (ImGui::IsItemEdited()) {
                    SendCommand(("clipboard_rotation_z|" + std::to_string(rotationZ * 90)).c_str());
                }
                */

                // MIRROR
                ImGui::Text("Mirror");
                ImGui::Checkbox("X##mirror_x", &mirrorX);
                if (ImGui::IsItemEdited()) {
                    SendCommand(("clipboard_mirror_x|" + std::string(mirrorX ? "true" : "false")).c_str());
                }
                ImGui::Checkbox("Y##mirror_y", &mirrorY);
                if (ImGui::IsItemEdited()) {
                    SendCommand(("clipboard_mirror_y|" + std::string(mirrorY ? "true" : "false")).c_str());
                }
                ImGui::Checkbox("Z##mirror_z", &mirrorZ);
                if (ImGui::IsItemEdited()) {
                    SendCommand(("clipboard_mirror_z|" + std::string(mirrorZ ? "true" : "false")).c_str());
                }

                // CLIP
                /*
                ImGui::Text("Clip");
                ImGui::Text("X");
                menu::IntInput("Min##clip_min_x", clipMinX, "clipboard_clip_min_x", typing);
                menu::IntInput("Max##clip_max_x", clipMaxX, "clipboard_clip_max_x", typing);

                ImGui::Text("Y");
                menu::IntInput("Min##clip_min_y", clipMinY, "clipboard_clip_min_y", typing);
                menu::IntInput("Max##clip_max_y", clipMaxY, "clipboard_clip_max_y", typing);

                ImGui::Text("Z");
                menu::IntInput("Min##clip_min_z", clipMinZ, "clipboard_clip_min_z", typing);
                menu::IntInput("Max##clip_max_z", clipMaxZ, "clipboard_clip_max_z", typing);
                */
            }
            /*if (ImGui::CollapsingHeader("Materials")) {

            } */        
            ImGui::End();
        }
    }
}