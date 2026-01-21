#include "Translate.h"
#include "MenuHelpers.h"
#include "pipe.h"

using namespace menu;

namespace Translate {

	static bool TranslateOwnMessagesEnabled = false;
	static char TranslateOwnMessagesLanguage[64] = "en";
	static bool TranslateTextCubesEnabled = false;
    static char TranslateCubesLanguage[64] = "en";

	void Render()
	{
        // translate own messages
		if (ImGui::Checkbox("Translate Own Messages", &TranslateOwnMessagesEnabled)) {
			if (TranslateOwnMessagesEnabled) SendCommand("translate_own_messages_enabled|true");
			else SendCommand("translate_own_messages_enabled|false");
		}

		if (TranslateOwnMessagesEnabled) {
			ImGui::Indent();
			ImGui::PushItemWidth(100);
			ImGui::InputText("Language##own", TranslateOwnMessagesLanguage, sizeof(TranslateOwnMessagesLanguage));
			ImGui::PopItemWidth();

			if (!typing) typing = ImGui::IsItemActive();

			if (ImGui::IsItemEdited()) {
				SendCommand((std::string("translate_own_messages_language|") + TranslateOwnMessagesLanguage).c_str());
			}
			ImGui::Unindent();
		}

        // translate text cubes
		if (ImGui::Checkbox("Translate Text Cubes", &TranslateTextCubesEnabled)) {
			if (TranslateTextCubesEnabled) SendCommand("translate_text_cubes_enabled|true");
			else SendCommand("translate_text_cubes_enabled|false");
		}

		if (TranslateTextCubesEnabled) {
			ImGui::Indent();
			ImGui::PushItemWidth(100);
			ImGui::InputText("Language##cubes", TranslateCubesLanguage, sizeof(TranslateCubesLanguage));
			ImGui::PopItemWidth();

			if (!typing) typing = ImGui::IsItemActive();

			if (ImGui::IsItemEdited()) {
				SendCommand((std::string("translate_text_cubes_language|") + TranslateCubesLanguage).c_str());
			}

			ImGui::SameLine();

			if (ImGui::Button("Translate")) SendCommand("translate_text_cubes_translate");

			ImGui::Unindent();
		}
	}

}