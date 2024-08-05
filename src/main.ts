import { readCSV } from "./read_csv"

interface Personnel {
    grade: grade_e
    nom: string
    prenom: string
    apte: boolean
    compagnie: number
}
interface Service {
    alias: services_e,
    grades: grade_e[],
    duration: number,
    effectif: number,
}

enum grade_e { sdt = 'sdt', cpl = 'cpl', cch = 'cch', cc1 = 'cc1', sgt = 'sgt', sch = 'sch', adj = 'adj', adc = 'adc', maj = 'maj', ltn = 'ltn', cne = 'cne' }
enum services_e { chef_ps = 'chef_ps', planton_ps = 'planton_ps', chef_spi = 'chef_spi', cpa = 'cpa', cp = 'cp', semaine = 'semaine', planton_ei = 'planton_ei', chef_ei = 'chef_ei' }

const services: Service[] = [
    {
        alias: services_e.planton_ps,
        grades: [grade_e.sdt, grade_e.cpl, grade_e.cch, grade_e.cc1],
        duration: 1,
        effectif: 2
    }
]
const mecs: Personnel[] = [
]

const GenererMoulinette = function () {
    for (let i = 0; i < 200; i++) {
        mecs.push({
            grade: getRandomEnumValue(grade_e),
            nom: 'SOLDAT_' + i,
            prenom: '_',
            apte: Math.random() > 0.1,
            compagnie: Math.floor(Math.random() * 9) + 1
        })
    }
}

const RecupererCSVMoulinette = function(){
    readCSV('./CSV/Effectifs.csv')
}

RecupererCSVMoulinette()

function getDaysInMonth(year: number, month: number): number {
    // Le mois suivant avec le jour 0 renvoie le dernier jour du mois précédent
    return new Date(year, month + 1, 0).getDate();
}
function getDaysInMonthFromDate(date: Date): number {
    const year = date.getFullYear();
    const month = date.getMonth();
    return getDaysInMonth(year, month);
}

const Affect = function (s: services_e) {
    const service = services.find(x => x.alias === s)!
    const nbServices = Math.ceil(28 / service.duration)

    const effectifTotal = mecs.filter(x => x.apte && service.grades.includes(x.grade)).length
    let personnel: Personnel[] = []
    let tries = 0;
    while (personnel.length < getDaysInMonthFromDate(new Date())) {
        if (tries++ > 100) {
            console.log('PAS ASSEZ DE PERSONNEL')
            break
        }
        for (let i = 1; i < 10; i++) {
            const compagnie = mecs.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade))
            const effectifCompagnie = compagnie.length
            const ratio = effectifCompagnie / effectifTotal
            const nbMecs = Math.ceil(ratio * service.effectif * 28)
            console.log('cie', i, 'donne', nbMecs)
            for (let m = 0; m < nbMecs; m++) {
                const mec = compagnie[Math.floor(Math.random() * compagnie.length)]
                if (personnel.some(x => x.nom === mec.nom)) {
                    console.log(mec.nom, 'deja utilised')
                    continue
                }
                personnel.push(mec)
            }
        }
    }

    personnel = personnel.sort((x, y) => Math.random() - 0.5)
    console.log(personnel.length)
    console.log(personnel[0])
    const container = $('#body')
    for (let d = 1; d <= getDaysInMonthFromDate(new Date()); d++) {
        const mec = personnel[d - 1]
        let hex = '#000000'
        switch (mec.compagnie) {
            case 1:
                hex = '#6DC0FF'
                break;
            case 2:
                hex = '#FF776D'
                break
            case 3:
                hex = '#FFE599'
                break
            case 4:
                hex = '#4BFF65'
                break
            case 5:
                hex = '#E52C85'
                break
            case 6:
                hex = '#FFA4A4'
                break;
            case 7:
                hex = '#FFFFFF'
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
        //console.log('JOUR', d, personnel[d - 1].nom, personnel[d - 1].compagnie, 'cie')
    }
}

export const GetBackCol = function (col: string, prefer_white: boolean = false) {
    const c = col.replace('#', '')
    const r = parseInt(c.substring(0, 2), 16) * 0.299
    const g = parseInt(c.substring(2, 4), 16) * 0.587
    const b = parseInt(c.substring(4, 6), 16) * 0.144
    if (r + g + b > (prefer_white ? 200 : 100)) return '#000'
    else return '#FFF'
  }
Affect(services_e.planton_ps)

function getRandomEnumValue<T>(enumObj: T): T[keyof T] {
    const enumValues = Object.values(enumObj) as T[keyof T][];
    const randomIndex = Math.floor(Math.random() * enumValues.length);
    return enumValues[randomIndex];
}
