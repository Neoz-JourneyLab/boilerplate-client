interface Personnel {
    id: string
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

function readCSVFile(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            resolve(reader.result as string);
        };
        reader.onerror = () => {
            reject(reader.error);
        };
        reader.readAsText(file);
    });
}

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('csvInput')!.addEventListener('change', async (event: Event) => {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files[0]) {
            try {
                mecs = []
                const data = await readCSVFile(input.files[0]);
                for (const l of data.split('\n')) {
                    if (l.startsWith('Nom;Prénom') || l.length === 0) continue
                    const field = l.split(';')
                    mecs.push({
                        id: field[0] + field[1] + field[2],
                        nom: field[0],
                        prenom: field[1],
                        grade: grade_e[field[2] as keyof typeof grade_e],
                        compagnie: parseInt(field[3]),
                        apte: field[4].startsWith('oui')
                    })
                }
                for (const s of Object.values(services_e)) {
                    console.log('gestion de', s)
                    Affect(s)
                }
            } catch (error) {
                console.error('Error reading CSV file:', error);
            }
        }
    });
})

enum grade_e { sdt = 'sdt', cpl = 'cpl', cch = 'cch', cc1 = 'cc1', sgt = 'sgt', sch = 'sch', adj = 'adj', adc = 'adc', maj = 'maj', ltn = 'ltn', cne = 'cne' }
enum services_e { chef_ps = 'chef_ps', planton_ps = 'planton_ps', chef_spi = 'chef_spi', cpa = 'cpa', cp = 'cp', semaine = 'semaine', planton_ei = 'planton_ei', chef_ei = 'chef_ei' }

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
let mecs: Personnel[] = []

function getDaysInMonth(year: number, month: number): number {
    // Le mois suivant avec le jour 0 renvoie le dernier jour du mois précédent
    return new Date(year, month + 1, 0).getDate();
}
function getDaysInMonthFromDate(date: Date): number {
    const year = date.getFullYear();
    const month = date.getMonth();
    return getDaysInMonth(year, month);
}
const ponctionnés: string[] = []
const Affect = function (s: services_e) {
    const service = services.find(x => x.alias === s)!
    const nbServices = Math.ceil(28 / service.duration)

    const effectifTotal = mecs.filter(x => x.apte && service.grades.includes(x.grade)).length
    let personnel: Personnel[] = []
    for (let i = 1; i < 10; i++) {
        const compagnie = mecs.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade))
        let dispo = mecs.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade) && !(ponctionnés.includes(x.id)))

        const effectifCompagnie = compagnie.length
        const ratio = effectifCompagnie / effectifTotal
        const nbMecs = Math.ceil(ratio * service.effectif * nbServices)
        console.log('cie', i, 'donne', nbMecs, 'pax/', effectifCompagnie, '(', effectifCompagnie, '/', effectifTotal, ')')
        for (let m = 0; m < nbMecs; m++) {
            if (dispo.length === 0) {
                console.log('pas de dispo pour cie', i)
                break
            }
            const mec = dispo[Math.floor(Math.random() * dispo.length)]
            ponctionnés.push(mec.id)
            personnel.push(mec)
            dispo = mecs.filter(x => x.apte && x.compagnie == i && service.grades.includes(x.grade) && !(ponctionnés.includes(x.id)))
        }
    }

    personnel = personnel.sort((x, y) => Math.random() - 0.5)
    const container = $('#body')
    for (let d = 1; d <= getDaysInMonthFromDate(new Date()); d += (service.duration)) {
        const mec = personnel[d - 1]
        if(!mec) {
            console.log('plus de mec dispo pour le jour', d, 'service', service.alias)
            break
        }
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

function getRandomEnumValue<T>(enumObj: T): T[keyof T] {
    const enumValues = Object.values(enumObj) as T[keyof T][];
    const randomIndex = Math.floor(Math.random() * enumValues.length);
    return enumValues[randomIndex];
}
