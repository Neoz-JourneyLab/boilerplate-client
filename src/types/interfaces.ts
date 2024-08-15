import {grade_e, services_e} from './enums'

export interface Personnel {
  id: string
  grade: grade_e
  nom: string
  prenom: string
  apte: boolean
  compagnie: number
}

export interface Service {
  alias: services_e,
  grades: grade_e[],
  duration: number,
  effectif: number,
}