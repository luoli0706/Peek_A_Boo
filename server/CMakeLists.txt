cmake_minimum_required(VERSION 3.20)
project(Peek-A-Boo-Server C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Server executable (ENet built directly from source + protobuf messages)
add_executable(server
    src/main.cpp
    src/protocol.cpp
    generated/peekaboo.pb.cc
    third_party/enet/enet.c
)

find_package(Protobuf REQUIRED)

target_include_directories(server PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${CMAKE_CURRENT_SOURCE_DIR}/generated
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/enet
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/enet/include
    ${Protobuf_INCLUDE_DIRS}
)

target_link_libraries(server PRIVATE ${Protobuf_LIBRARIES})

if (WIN32)
    target_link_libraries(server PRIVATE ws2_32 winmm)
endif()
