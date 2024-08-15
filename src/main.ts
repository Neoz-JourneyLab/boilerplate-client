import { Personnel, Service } from './types/interfaces'
import { grade_e, services_e } from './types/enums'
import { GetBackCol, getDaysInMonthFromDate, readCSVFile } from './common/common'

const services: Service[] = [
  {
    alias: services_e.planton_ps,
    grades: [grade_e.sdt, grade_e.cpl, grade_e.cch, grade_e.cc1],
    duration: 1,
    effectif: 2
  },
  {
    alias: services_e.chef_ps,
    grades: [grade_e.sgt],
    duration: 1,
    effectif: 1
  },
  {
    alias: services_e.semaine,
    grades: [grade_e.sgt, grade_e.cch, grade_e.cc1],
    duration: 1,
    effectif: 1
  },
  {
    alias: services_e.chef_spi,
    grades: [grade_e.sgt],
    duration: 1,
    effectif: 1
  },
  {
    alias: services_e.cpa,
    grades: [grade_e.sch],
    duration: 1,
    effectif: 1
  },
  {
    alias: services_e.cp,
    grades: [grade_e.adj, grade_e.adc, grade_e.maj, grade_e.ltn],
    duration: 1,
    effectif: 1
  },
  {
    alias: services_e.chef_ei,
    grades: [grade_e.sgt, grade_e.cch, grade_e.cc1],
    duration: 1,
    effectif: 1
  },
  {
    alias: services_e.planton_ei,
    grades: [grade_e.sdt, grade_e.cpl, grade_e.cch, grade_e.cc1],
    duration: 1,
    effectif: 4
  },
]
const personnel: Personnel[] = []
const taken: string[] = []

document.addEventListener('DOMContentLoaded', () => {
  document.getElementById('csvInput')!.addEventListener('change', async (event: Event) => {
    const input = event.target as HTMLInputElement
    if (!input.files || !input.files[0]) return
    try {
      personnel.slice(0, personnel.length - 1)
      taken.slice(0, taken.length - 1)
      const data = await readCSVFile(input.files[0])
      for (const l of data.split('\n')) {
        if (l.startsWith('Nom;Prénom') || l.length === 0) continue
        const field = l.split(';')
        personnel.push({
          id: field[0] + field[1] + field[2],
          nom: field[0],
          prenom: field[1],
          grade: grade_e[field[2] as keyof typeof grade_e],
          compagnie: parseInt(field[3]),
          apte: field[4].startsWith('oui')
        })
      }
      for (const s: services_e of Object.values(services_e)) {
        Affect(s)
      }
    } catch (error) {
      console.error('Error reading CSV file:', error)
    }
  })
  for (const s: services_e of Object.values(services_e)) {
    document.getElementById(s.toString())!.addEventListener('click', () => {
      $('.table').attr('hidden', 'hidden')
      $('#table-' + s.toString()).removeAttr('hidden')
      $('.nav-link').removeClass('active')
      $('#' + s.toString()).addClass('active')
      console.log('table-' + s.toString())
    })
  }
})

const Affect = function (s: services_e) {
  const service = services.find(x => x.alias === s)!
  const nbServices = Math.ceil(28 / service.duration)

  const total = personnel.filter(x => x.apte && service.grades.includes(x.grade)).length
  const retenus_pour_service: Personnel[] = []
  for (let i = 1; i < 10; i++) {
    const company = personnel.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade))
    let available = personnel.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade) && !(taken.includes(x.id)))

    const companyAmount = company.length
    const ratio = companyAmount / total
    const nbMecs = Math.ceil(ratio * service.effectif * nbServices)
    for (let m = 0; m < nbMecs; m++) {
      if (available.length === 0) {
        break
      }
      const mec = available[Math.floor(Math.random() * available.length)]
      taken.push(mec.id)
      retenus_pour_service.push(mec)
      available = personnel.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade) && !(taken.includes(x.id)))
    }
  }

  const random_soldier = retenus_pour_service.sort((x, y) => Math.random() - 0.5)
  const container = $('#body-' + service.alias.toString())
  for (let d = 1; d <= getDaysInMonthFromDate(new Date()); d += (service.duration)) {
    const mec = random_soldier[d - 1]
    if (!mec) {
      const tr = $('<tr>')
      tr.append($('<td>').text(d))
      tr.append($('<td>').text('/'))
      tr.append($('<td>').text('PAS DE DISPO').css({ 'background-color': 'red' }))
      tr.append($('<td>').text('/'))
      tr.append($('<td>').text('/'))
      tr.append($('<td>').text(s.toString()))
      container.append(tr)
      continue
    }
    let hex = '#000000'
    switch (mec.compagnie) {
      case 1:
        hex = '#1993ef'
        break
      case 2:
        hex = '#db1a0f'
        break
      case 3:
        hex = '#ffdd00'
        break
      case 4:
        hex = '#4BFF65'
        break
      case 5:
        hex = '#d016bc'
        break
      case 6:
        hex = '#ffadad'
        break
      case 7:
        hex = '#ffffff'
        break
      case 8:
        hex = '#99FFF8'
        break
      case 9:
        hex = '#CB7D3B'
        break
    }

    const tr = $('<tr>')
    tr.append($('<td>').text(d))
    tr.append($('<td>').text(mec.nom))
    tr.append($('<td>').text(mec.prenom))
    tr.append($('<td>').text(mec.grade))
    tr.append($('<td>').text(mec.compagnie).css({ 'background-color': hex, 'color': GetBackCol(hex, true) }))
    tr.append($('<td>').text(s.toString()))
    container.append(tr)
  }
}

