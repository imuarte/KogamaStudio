#pragma once
#include <string>
#include <vector>

namespace Explorer {
    struct ObjectEntry {
        std::string id;
        std::string type;
        std::string name;
        int itemId = -1;
    };

    void LoadFromFile(const std::string& path);
    void SetSelected(const std::string& id);
    void Render();
}
