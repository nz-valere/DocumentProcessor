interface ProgressBarProps {
  value: number; // Percentage value (0-100)
}

export default function ProgressBar({ value }: ProgressBarProps) {
  const progress = Math.max(0, Math.min(100, value)); // Ensure value is between 0 and 100

  return (
    <div className="w-full bg-gray-200 rounded shadow-inner">
      <div 
        className="h-5 bg-green-500 rounded text-center text-white text-sm font-bold leading-5 transition-width duration-300"
        style={{ width: `${progress}%` }}
      >
        {`${Math.round(progress)}%`}
      </div>
    </div>
  );
}