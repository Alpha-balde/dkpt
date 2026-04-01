export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  email: string
  role: string
}

export interface User {
  id: string
  email: string
  role: 'Admin' | 'Secretaire' | 'Tresorier' | 'Lecteur'
  createdAt: string
}

export interface Member {
  id: string
  numeroMembre: string
  prenom: string
  nom: string
  telephone: string | null
  whatsApp: string | null
  residence: string | null
  village: string | null
  sousPrefecture: string | null
  anneeDebut: number
  actif: boolean
  createdAt: string
}

export interface MemberSimple {
  id: string
  numeroMembre: string
  prenom: string
  nom: string
}

export interface Payment {
  id: string
  memberId: string
  annee: number
  datePaiement: string
  montant: number
  fraisPaiement: number
  moyenPaiement: string
  reference: string | null
  note: string | null
  createdAt: string
  member: MemberSimple | null
}

export interface ContributionAmount {
  year: number
  amount: number
}

export interface Setting {
  id: number
  montantCotisationAnnuelleParDefaut: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface DashboardStats {
  totalMembers: number
  totalCollected: number
  totalFees: number
  membersInOrder: number
  membersNotInOrder: number
}
