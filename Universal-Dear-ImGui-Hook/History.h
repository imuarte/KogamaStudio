#pragma once
#include <string>

namespace History {
    void Add(const std::string& action);
    void AddTransform(const std::string& objectId,
                      float oldX, float oldY, float oldZ,
                      float newX, float newY, float newZ);
    void AddRotation(const std::string& objectId,
                     float oldQx, float oldQy, float oldQz, float oldQw,
                     float newQx, float newQy, float newQz, float newQw);
    void Render();
}
