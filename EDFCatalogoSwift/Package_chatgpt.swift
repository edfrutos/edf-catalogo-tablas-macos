// swift-tools-version:5.5
import PackageDescription

let package = Package(
    name: "EDFCatalogoSwift",
    platforms: [
        .macOS(.v12)
    ],
    products: [
        .executable(name: "EDFCatalogoSwift", targets: ["EDFCatalogoSwift"])
    ],
    dependencies: [
        .package(url: "https://github.com/mongodb/mongo-swift-driver.git", from: "1.3.1"),
        .package(url: "https://github.com/apple/swift-argument-parser.git", from: "1.2.0")
    ],
    targets: [
        .executableTarget(
            name: "EDFCatalogoSwift",
            dependencies: [
                .product(name: "MongoSwift", package: "mongo-swift-driver"),
                .product(name: "ArgumentParser", package: "swift-argument-parser")
            ],
            path: "Sources"
        )
    ]
)


// Ejecutar parche automático del CLibMongoC tras la compilación
import Foundation

let fixScriptPath = FileManager.default.currentDirectoryPath + "/fix_clibmongoc.sh"
if FileManager.default.fileExists(atPath: fixScriptPath) {
    let process = Process()
    process.executableURL = URL(fileURLWithPath: "/bin/bash")
    process.arguments = [fixScriptPath]
    try? process.run()
}
