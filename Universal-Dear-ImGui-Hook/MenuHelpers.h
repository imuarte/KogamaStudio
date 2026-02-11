#pragma once
#include <string>

namespace menu
{
	extern bool typing;

	void OpenFolder(std::string subpath);
	void SendCommand(const char* cmd);
	void DisplayDirTree(const std::string& path, const std::string& rel = "");
	void FloatInput(const char* label, float& value, const char* commandPrefix, bool& typing, float width = 100.0f);
	void IntInput(const char* label, int& value, const char* commandPrefix, bool& typing, float width = 100.0f);
}
