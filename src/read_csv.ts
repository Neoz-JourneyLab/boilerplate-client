import fs from 'fs';
import csv from 'csv-parser';

export const readCSV = (filePath: string) => {
  fs.createReadStream(filePath)
    .pipe(csv())
    .on('data', (row) => {
      console.log(row);
    })
    .on('end', () => {
      console.log('CSV file successfully processed');
    });
};

//readCSV('./CSV/Effectifs.csv'); // Remplacez par le chemin de votre fichier CSV
