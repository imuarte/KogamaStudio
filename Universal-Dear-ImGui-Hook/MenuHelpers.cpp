#include "stdafx.h"
#include "MenuHelpers.h"
#include "pipe.h"
#include <cstdlib>

namespace menu {

    bool typing = false;

    void SendCommand(const char* cmd)
    {
        HANDLE hPipe = CreateFileA(
            "\\\\.\\pipe\\KogamaStudio",
            GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL
        );
        if (hPipe != INVALID_HANDLE_VALUE) {
            DWORD written;
            WriteFile(hPipe, cmd, strlen(cmd), &written, NULL);
            CloseHandle(hPipe);
            DebugLog("[menu] Command sent: %s\n", cmd);
        }
        else {
            DebugLog("[menu] Pipe not connected\n");
        }
    }

    void OpenFolder(std::string subpath)
    {
        char* buffer = nullptr;
        size_t len;
        _dupenv_s(&buffer, &len, "APPDATA");
        std::string path = std::string(buffer) + "\\..\\Local\\KogamaStudio\\" + subpath;
        free(buffer);

        std::string cmd = "explorer \"" + path + "\"";
        system(cmd.c_str());
    }

    void DisplayDirTree(const std::string& path, const std::string& rel) {
        WIN32_FIND_DATAA fd;
        HANDLE h = FindFirstFileA((path + "\\*").c_str(), &fd);
        if (h == INVALID_HANDLE_VALUE) return;

        do {
            if (fd.cFileName[0] == '.') continue;
            std::string name = fd.cFileName;
            std::string full = path + "\\" + name;
            std::string relPath = rel.empty() ? name : rel + "/" + name;

            if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
                if (ImGui::TreeNode(name.c_str()))
                    DisplayDirTree(full, relPath), ImGui::TreePop();
            }
            else {
                ImGui::Text(name.c_str());
                ImGui::SameLine();
                if (ImGui::Button(("Generate##" + name).c_str()))
                    SendCommand(("generate_model|" + relPath).c_str());
            }
        } while (FindNextFileA(h, &fd));
        FindClose(h);
    }

}