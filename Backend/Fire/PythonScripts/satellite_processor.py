#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
YANDES - Uydu Veri İşleme Script'i
İzmir Yangın verilerini işler ve analiz eder
"""

import sys
import os
import json
import numpy as np
import rasterio
from rasterio.mask import mask
from rasterio.warp import calculate_default_transform, reproject, Resampling
import matplotlib.pyplot as plt
from matplotlib.colors import LinearSegmentedColormap
import geopandas as gpd
from shapely.geometry import box
import warnings
warnings.filterwarnings('ignore')

class SatelliteDataProcessor:
    def __init__(self, data_root_path):
        self.data_root_path = data_root_path
        self.results_dir = os.path.join(data_root_path, "processed_results")
        os.makedirs(self.results_dir, exist_ok=True)
        
    def process_sentinel1_data(self, data_path):
        """Sentinel-1 SAR verilerini işler"""
        try:
            # SAFE dosya yapısını analiz et
            manifest_path = os.path.join(data_path, "manifest.safe")
            if not os.path.exists(manifest_path):
                return {"success": False, "error": "Manifest dosyası bulunamadı"}
            
            # Measurement klasöründeki veri dosyalarını bul
            measurement_dir = os.path.join(data_path, "measurement")
            if not os.path.exists(measurement_dir):
                return {"success": False, "error": "Measurement klasörü bulunamadı"}
            
            # VV ve VH polarizasyon dosyalarını bul
            vv_files = [f for f in os.listdir(measurement_dir) if "VV" in f and f.endswith(".tiff")]
            vh_files = [f for f in os.listdir(measurement_dir) if "VH" in f and f.endswith(".tiff")]
            
            result = {
                "success": True,
                "satellite_type": "Sentinel-1",
                "data_type": "SAR",
                "available_polarizations": [],
                "file_info": {}
            }
            
            if vv_files:
                result["available_polarizations"].append("VV")
                result["file_info"]["VV"] = {
                    "file": vv_files[0],
                    "size_mb": os.path.getsize(os.path.join(measurement_dir, vv_files[0])) / (1024*1024)
                }
            
            if vh_files:
                result["available_polarizations"].append("VH")
                result["file_info"]["VH"] = {
                    "file": vh_files[0],
                    "size_mb": os.path.getsize(os.path.join(measurement_dir, vh_files[0])) / (1024*1024)
                }
            
            return result
            
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def process_landsat_data(self, data_path):
        """Landsat 8&9 optik verilerini işler"""
        try:
            # TIF dosyalarını bul
            tif_files = [f for f in os.listdir(data_path) if f.endswith(".TIF")]
            
            if not tif_files:
                return {"success": False, "error": "TIF dosyası bulunamadı"}
            
            result = {
                "success": True,
                "satellite_type": "Landsat8&9",
                "data_type": "Optical",
                "available_bands": [],
                "band_info": {},
                "total_size_mb": 0
            }
            
            # Her band için bilgi topla
            for tif_file in tif_files:
                band_name = tif_file.replace(".TIF", "")
                file_path = os.path.join(data_path, tif_file)
                file_size_mb = os.path.getsize(file_path) / (1024*1024)
                
                result["available_bands"].append(band_name)
                result["band_info"][band_name] = {
                    "file": tif_file,
                    "size_mb": round(file_size_mb, 2)
                }
                result["total_size_mb"] += file_size_mb
            
            result["total_size_mb"] = round(result["total_size_mb"], 2)
            
            return result
            
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def process_sentinel2_data(self, data_path):
        """Sentinel-2 optik verilerini işler"""
        try:
            # JP2 dosyalarını bul
            jp2_files = [f for f in os.listdir(data_path) if f.endswith(".jp2")]
            
            if not jp2_files:
                return {"success": False, "error": "JP2 dosyası bulunamadı"}
            
            result = {
                "success": True,
                "satellite_type": "Sentinel-2",
                "data_type": "Optical",
                "available_bands": [],
                "band_info": {},
                "resolutions": {"10m": [], "20m": []},
                "total_size_mb": 0
            }
            
            # Her band için bilgi topla
            for jp2_file in jp2_files:
                band_name = jp2_file.replace(".jp2", "")
                file_path = os.path.join(data_path, jp2_file)
                file_size_mb = os.path.getsize(file_path) / (1024*1024)
                
                # Çözünürlüğü belirle
                if "10m" in band_name:
                    resolution = "10m"
                elif "20m" in band_name:
                    resolution = "20m"
                else:
                    resolution = "unknown"
                
                result["available_bands"].append(band_name)
                result["band_info"][band_name] = {
                    "file": jp2_file,
                    "size_mb": round(file_size_mb, 2),
                    "resolution": resolution
                }
                
                if resolution in result["resolutions"]:
                    result["resolutions"][resolution].append(band_name)
                
                result["total_size_mb"] += file_size_mb
            
            result["total_size_mb"] = round(result["total_size_mb"], 2)
            
            return result
            
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def calculate_ndvi(self, red_band_path, nir_band_path, output_path):
        """NDVI (Normalized Difference Vegetation Index) hesaplar"""
        try:
            with rasterio.open(red_band_path) as red_src, rasterio.open(nir_band_path) as nir_src:
                red = red_src.read(1).astype(np.float32)
                nir = nir_src.read(1).astype(np.float32)
                
                # NDVI hesapla: (NIR - Red) / (NIR + Red)
                ndvi = np.where((nir + red) != 0, (nir - red) / (nir + red), 0)
                
                # -1 ile 1 arasında sınırla
                ndvi = np.clip(ndvi, -1, 1)
                
                # Sonucu kaydet
                profile = red_src.profile
                profile.update(dtype=rasterio.float32, count=1)
                
                with rasterio.open(output_path, 'w', **profile) as dst:
                    dst.write(ndvi.astype(rasterio.float32), 1)
                
                # İstatistikler
                stats = {
                    "min": float(np.nanmin(ndvi)),
                    "max": float(np.nanmax(ndvi)),
                    "mean": float(np.nanmean(ndvi)),
                    "std": float(np.nanstd(ndvi))
                }
                
                return {"success": True, "stats": stats, "output_path": output_path}
                
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def calculate_nbr(self, nir_band_path, swir2_band_path, output_path):
        """NBR (Normalized Burn Ratio) hesaplar"""
        try:
            with rasterio.open(nir_band_path) as nir_src, rasterio.open(swir2_band_path) as swir2_src:
                nir = nir_src.read(1).astype(np.float32)
                swir2 = swir2_src.read(1).astype(np.float32)
                
                # NBR hesapla: (NIR - SWIR2) / (NIR + SWIR2)
                nbr = np.where((nir + swir2) != 0, (nir - swir2) / (nir + swir2), 0)
                
                # -1 ile 1 arasında sınırla
                nbr = np.clip(nbr, -1, 1)
                
                # Sonucu kaydet
                profile = nir_src.profile
                profile.update(dtype=rasterio.float32, count=1)
                
                with rasterio.open(output_path, 'w', **profile) as dst:
                    dst.write(nbr.astype(rasterio.float32), 1)
                
                # İstatistikler
                stats = {
                    "min": float(np.nanmin(nbr)),
                    "max": float(np.nanmax(nbr)),
                    "mean": float(np.nanmean(nbr)),
                    "std": float(np.nanstd(nbr))
                }
                
                return {"success": True, "stats": stats, "output_path": output_path}
                
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def create_fire_analysis_report(self, before_data_path, after_data_path, analysis_type):
        """Yangın analiz raporu oluşturur"""
        try:
            report = {
                "success": True,
                "analysis_type": analysis_type,
                "before_data": before_data_path,
                "after_data": after_data_path,
                "analysis_date": str(np.datetime64('now')),
                "results": {}
            }
            
            if analysis_type == "NDVI":
                # NDVI analizi
                before_ndvi = self._calculate_simple_ndvi(before_data_path)
                after_ndvi = self._calculate_simple_ndvi(after_data_path)
                
                if before_ndvi["success"] and after_ndvi["success"]:
                    change = after_ndvi["mean"] - before_ndvi["mean"]
                    report["results"] = {
                        "before_ndvi_mean": before_ndvi["mean"],
                        "after_ndvi_mean": after_ndvi["mean"],
                        "change": change,
                        "vegetation_loss_percent": max(0, -change * 100)
                    }
            
            elif analysis_type == "NBR":
                # NBR analizi
                before_nbr = self._calculate_simple_nbr(before_data_path)
                after_nbr = self._calculate_simple_nbr(after_data_path)
                
                if before_nbr["success"] and after_nbr["success"]:
                    change = after_nbr["mean"] - before_nbr["mean"]
                    report["results"] = {
                        "before_nbr_mean": before_nbr["mean"],
                        "after_nbr_mean": after_nbr["mean"],
                        "change": change,
                        "burn_severity": self._classify_burn_severity(change)
                    }
            
            return report
            
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def _calculate_simple_ndvi(self, data_path):
        """Basit NDVI hesaplama (tüm bandları kullanarak)"""
        try:
            # Red ve NIR bandlarını bul
            red_band = None
            nir_band = None
            
            for file in os.listdir(data_path):
                if file.endswith(".TIF") or file.endswith(".jp2"):
                    if "Band4" in file or "B04" in file:  # Red
                        red_band = os.path.join(data_path, file)
                    elif "Band5" in file or "B08" in file:  # NIR
                        nir_band = os.path.join(data_path, file)
            
            if red_band and nir_band:
                return self.calculate_ndvi(red_band, nir_band, 
                                         os.path.join(self.results_dir, "temp_ndvi.tif"))
            else:
                return {"success": False, "error": "Red veya NIR band bulunamadı"}
                
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def _calculate_simple_nbr(self, data_path):
        """Basit NBR hesaplama"""
        try:
            # NIR ve SWIR2 bandlarını bul
            nir_band = None
            swir2_band = None
            
            for file in os.listdir(data_path):
                if file.endswith(".TIF") or file.endswith(".jp2"):
                    if "Band5" in file or "B08" in file:  # NIR
                        nir_band = os.path.join(data_path, file)
                    elif "Band7" in file or "B12" in file:  # SWIR2
                        swir2_band = os.path.join(data_path, file)
            
            if nir_band and swir2_band:
                return self.calculate_nbr(nir_band, swir2_band,
                                        os.path.join(self.results_dir, "temp_nbr.tif"))
            else:
                return {"success": False, "error": "NIR veya SWIR2 band bulunamadı"}
                
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def _classify_burn_severity(self, nbr_change):
        """NBR değişimine göre yanma şiddetini sınıflandırır"""
        if nbr_change > -0.1:
            return "Low"
        elif nbr_change > -0.27:
            return "Moderate"
        elif nbr_change > -0.44:
            return "High"
        else:
            return "Very High"

def main():
    """Ana fonksiyon - komut satırından çağrılır"""
    if len(sys.argv) < 3:
        print("Kullanım: python satellite_processor.py <command> <data_path> [options]")
        print("Komutlar:")
        print("  sentinel1 <data_path> - Sentinel-1 verilerini işle")
        print("  landsat <data_path> - Landsat verilerini işle")
        print("  sentinel2 <data_path> - Sentinel-2 verilerini işle")
        print("  ndvi <red_band> <nir_band> <output> - NDVI hesapla")
        print("  nbr <nir_band> <swir2_band> <output> - NBR hesapla")
        print("  analyze <before_path> <after_path> <type> - Yangın analizi")
        return
    
    command = sys.argv[1]
    processor = SatelliteDataProcessor(".")
    
    if command == "sentinel1":
        result = processor.process_sentinel1_data(sys.argv[2])
        print(json.dumps(result, indent=2, ensure_ascii=False))
    
    elif command == "landsat":
        result = processor.process_landsat_data(sys.argv[2])
        print(json.dumps(result, indent=2, ensure_ascii=False))
    
    elif command == "sentinel2":
        result = processor.process_sentinel2_data(sys.argv[2])
        print(json.dumps(result, indent=2, ensure_ascii=False))
    
    elif command == "ndvi":
        if len(sys.argv) < 5:
            print("NDVI için: python satellite_processor.py ndvi <red_band> <nir_band> <output>")
            return
        result = processor.calculate_ndvi(sys.argv[2], sys.argv[3], sys.argv[4])
        print(json.dumps(result, indent=2, ensure_ascii=False))
    
    elif command == "nbr":
        if len(sys.argv) < 5:
            print("NBR için: python satellite_processor.py nbr <nir_band> <swir2_band> <output>")
            return
        result = processor.calculate_nbr(sys.argv[2], sys.argv[3], sys.argv[4])
        print(json.dumps(result, indent=2, ensure_ascii=False))
    
    elif command == "analyze":
        if len(sys.argv) < 5:
            print("Analiz için: python satellite_processor.py analyze <before_path> <after_path> <type>")
            return
        result = processor.create_fire_analysis_report(sys.argv[2], sys.argv[3], sys.argv[4])
        print(json.dumps(result, indent=2, ensure_ascii=False))
    
    else:
        print(f"Bilinmeyen komut: {command}")

if __name__ == "__main__":
    main()
