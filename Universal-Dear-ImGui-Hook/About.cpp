#include "About.h"
#include "version.h"

namespace About {
	void Render() {
        ImGui::Text(u8"KogamaStudio v" KS_VERSION);
        ImGui::Spacing();
        ImGui::Separator();
        ImGui::Spacing();
        ImGui::Text(T(u8"Special Thanks to:"));
        ImGui::BulletText(u8"Sh0ckFR - Universal-Dear-ImGui-Hook");
        ImGui::BulletText(u8"Beckowl - KogamaTools");
        ImGui::BulletText(u8"ocornut - Dear ImGui");
        ImGui::Spacing();
	}
}
