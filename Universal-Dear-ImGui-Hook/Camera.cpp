#include "stdafx.h"
#include "Camera.h"
#include "MenuHelpers.h"
#include "UILocale.h"

using namespace menu;

namespace CameraPanel {

    static bool freecamEnabled = false;
    static float freecamSpeed = 20.0f;
    static float freecamSensitivity = 2.0f;
    static bool customFovEnabled = false;
    static float fov = 60.0f;
    static bool customDistanceEnabled = false;
    static float cameraDistance = 5.0f;

    void Render()
    {
        ImGuiWindowFlags flags = ImGuiWindowFlags_NoCollapse;
        if (ImGui::Begin((std::string(T(u8"View")) + u8"###Camera").c_str(), nullptr, flags))
        {
            // freecam
            if (ImGui::Checkbox(T(u8"Freecam"), &freecamEnabled))
            {
                if (freecamEnabled) SendCommand(u8"camera_freecam_enable");
                else                SendCommand(u8"camera_freecam_disable");
            }

            if (freecamEnabled)
            {
                ImGui::Indent();
                DragFloatInput(T(u8"Speed##fcam_speed"), freecamSpeed, u8"camera_freecam_speed", typing, 0.5f, 120.0f, u8"%.1f");
                DragFloatInput(T(u8"Sensitivity##fcam_sens"), freecamSensitivity, u8"camera_freecam_sensitivity", typing, 0.05f, 120.0f, u8"%.2f");
                ImGui::Unindent();
            }

            ImGui::Separator();

            // fov
            if (ImGui::Checkbox(T(u8"Custom FOV"), &customFovEnabled))
            {
                if (customFovEnabled) SendCommand((std::string(u8"camera_fov|") + std::to_string(fov)).c_str());
                else                  SendCommand(u8"camera_fov_reset");
            }

            if (customFovEnabled)
            {
                ImGui::Indent();
                DragFloatInput(u8"FOV##cam_fov", fov, u8"camera_fov", typing, 0.5f, 120.0f, u8"%.1f");
                ImGui::Unindent();
            }

            ImGui::Separator();

            // custom distance
            if (ImGui::Checkbox(T(u8"Custom Camera Distance"), &customDistanceEnabled))
            {
                if (customDistanceEnabled)
                    SendCommand((std::string(u8"camera_distance|") + std::to_string(cameraDistance)).c_str());
                else
                    SendCommand(u8"camera_distance_reset");
            }

            if (customDistanceEnabled)
            {
                ImGui::Indent();
                DragFloatInput(u8"Distance##cam_dist", cameraDistance, u8"camera_distance", typing, 0.1f, 120.0f, u8"%.1f");
                ImGui::Unindent();
            }
        }
        ImGui::End();
    }

}
