cmake_minimum_required(VERSION 3.20)
project(Peek-A-Boo-Server C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Server executable (ENET built directly from ENet-CSharp single-file source)
add_executable(server
    src/main.cpp
    src/protocol.cpp
    deps/enet/enet.c
)

target_include_directories(server PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/deps/enet
)

if (WIN32)
    target_link_libraries(server PRIVATE ws2_32 winmm)
endif()
