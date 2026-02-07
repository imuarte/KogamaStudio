#include "Generate.h"
#include "pipe.h"
#include "MenuHelpers.h"

using namespace menu;

namespace Generate {

    static int targetId = -1;

	void Render()
	{

        if (ImGui::Button("Open Folder")) OpenFolder("Generate\\Models");

        //ImGui::PushItemWidth(100);
        //ImGui::InputInt("ID", &targetId);
        //ImGui::PopItemWidth();

        //if (!typing) typing = ImGui::IsItemActive();

        //if (ImGui::Button("Save to file")) SendCommand(("generate_save_model_to_file|" + std::to_string(targetId)).c_str());

        ImGui::Spacing();
        ImGui::Separator();
        ImGui::Spacing();

        if (pipe::generateProgress == 1.0f || pipe::generateProgress == 0.0f) {
            // showing available models
            char path[MAX_PATH];
            ExpandEnvironmentStringsA("%LOCALAPPDATA%\\KogamaStudio\\Generate\\Models", path, MAX_PATH);
            DisplayDirTree(path);
        }
        else
        {
            ImGui::Text("Progress");
            ImGui::SameLine();

            ImGui::ProgressBar(pipe::generateProgress, ImVec2(-100, 0));
            ImGui::SameLine();
            if (ImGui::Button("Cancel")) {
                SendCommand("generate_cancel");
                pipe::generateProgress = 0.0f;
            }
        }
	}
}