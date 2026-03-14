#include "About.h"
#include "MenuHelpers.h"
#include "version.h"

namespace About {
	void Render() {
        if (!ImGui::Begin((std::string(T(u8"About")) + u8"###About").c_str(), nullptr, ImGuiWindowFlags_NoCollapse))
        {
            ImGui::End();
            return;
        }

        ImGui::Text(u8"KogamaStudio v" KS_VERSION);
        ImGui::Spacing();
        ImGui::Separator();
        ImGui::Spacing();
        ImGui::Text(u8"Special thanks to Becko, MauryDev");
        ImGui::Spacing();

        ImGui::End();
	}
}
