import SwiftUI

struct ProfileView: View {
    @EnvironmentObject private var authViewModel: AuthViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            if let user = authViewModel.currentUser {
                HStack {
                    Text("Nombre:")
                        .bold()
                    Text(user.name)
                }
                HStack {
                    Text("Email:")
                        .bold()
                    Text(user.email)
                }
                HStack {
                    Text("Rol:")
                        .bold()
                    Text(user.isAdmin ? "Administrador" : "Usuario")
                }
            } else {
                Text("No hay usuario autenticado")
                    .foregroundColor(.secondary)
            }

            Spacer()

            Button("Cerrar sesión") {
                authViewModel.signOut()
            }
        }
        .padding()
        .frame(maxWidth: .infinity, alignment: .leading)
        .navigationTitle("Perfil")
    }
}

struct ProfileView_Previews: PreviewProvider {
    static var previews: some View {
        ProfileView()
            .environmentObject(AuthViewModel())
    }
}
