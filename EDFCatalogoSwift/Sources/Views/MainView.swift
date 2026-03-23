import SwiftUI

struct MainView: View {
    @EnvironmentObject var authViewModel: AuthViewModel
    @State private var selectedTab: Int? = 0

    var body: some View {
        NavigationSplitView {
            sidebar
        } detail: {
            if let selectedTab {
                switch selectedTab {
                case 0: CatalogsView()
                case 1: ProfileView()
                case 2: AdminView()
                default: emptyState
                }
            } else {
                emptyState
            }
        }
    }

    var sidebar: some View {
        List(selection: $selectedTab) {
            NavigationLink(value: 0) {
                Label("Catálogos", systemImage: "folder")
            }

            NavigationLink(value: 1) {
                Label("Perfil", systemImage: "person")
            }

            if (authViewModel.currentUser?.isAdmin ?? false) {
                NavigationLink(value: 2) {
                    Label("Administración", systemImage: "gear")
                }
            }

            Menu {
                Button(role: .destructive) {
                    authViewModel.signOut()
                } label: {
                    Label("Cerrar sesión", systemImage: "rectangle.portrait.and.arrow.right")
                }
            } label: {
                Label("Cuenta", systemImage: "person.crop.circle")
            }
        }
        .listStyle(.sidebar)
    }

    private var emptyState: some View {
        VStack(spacing: 12) {
            Image(systemName: "arrow.left")
                .font(.system(size: 40))
                .foregroundStyle(.secondary)
            Text("Seleccione una opción")
                .font(.title2)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }
}
