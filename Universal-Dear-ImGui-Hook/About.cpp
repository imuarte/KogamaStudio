#include "About.h"

namespace About {
	void Render() {
        ImGui::Text("KogamaStudio v0.3.0");
        ImGui::Spacing();
        ImGui::Separator();
        ImGui::Spacing();
        ImGui::Text("Special Thanks to:");
        ImGui::BulletText("Sh0ckFR - Universal-Dear-ImGui-Hook");
        ImGui::BulletText("Beckowl - KogamaTools");
        ImGui::BulletText("ocornut - Dear ImGui");
        ImGui::Spacing();
	}
}